// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using Xunit;
using DicomFileExtensions = Microsoft.Health.Dicom.Core.Features.Retrieve.DicomFileExtensions;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Update;

public class UpdateInstanceServiceTests
{
    private readonly IFileStore _fileStore;
    private readonly ILogger<UpdateInstanceService> _logger;
    private readonly IMetadataStore _metadataStore;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly UpdateInstanceService _updateInstanceService;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private static readonly FileProperties DefaultFileProperties = new FileProperties()
    {
        Path = String.Empty,
        ETag = String.Empty
    };

    public UpdateInstanceServiceTests()
    {
        _fileStore = Substitute.For<IFileStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _logger = NullLogger<UpdateInstanceService>.Instance;
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _dicomRequestContextAccessor.RequestContext.DataPartition = Partition.Default;
        var config = new UpdateConfiguration();

        _updateInstanceService = new UpdateInstanceService(
            _fileStore,
            _metadataStore,
            _recyclableMemoryStreamManager,
            Options.Create(config),
            _logger);
    }

    [Fact]
    public async Task GivenDatasetToUpdateIsNull_WhenCalled_ThrowsArgumentNullException()
    {
        InstanceFileState instanceFileIdentifier = new InstanceFileState();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _updateInstanceService.UpdateInstanceBlobAsync(instanceFileIdentifier, null, Partition.Default, CancellationToken.None));
    }

    [Fact]
    public async Task GivenInstanceFileIdentifierIsNull_WhenCalled_ThrowsArgumentNullException()
    {
        DicomDataset datasetToUpdate = new DicomDataset();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _updateInstanceService.UpdateInstanceBlobAsync(null, datasetToUpdate, Partition.Default, CancellationToken.None));
    }

    [Fact]
    public async Task GivenNewVersionIsNull_WhenCalled_ThrowsArgumentException()
    {
        InstanceFileState instanceFileIdentifier = new InstanceFileState
        {
            NewVersion = null
        };
        DicomDataset datasetToUpdate = new DicomDataset();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _updateInstanceService.UpdateInstanceBlobAsync(instanceFileIdentifier, datasetToUpdate, Partition.Default, CancellationToken.None));
    }

    [Fact]
    public async Task GivenValidInput_WhenDeletingBothFileAndMetadata_ThenItDeletes()
    {
        long fileIdentifier = 1234;
        await _updateInstanceService.DeleteInstanceBlobAsync(fileIdentifier, Partition.Default);
        await _fileStore.Received(1).DeleteFileIfExistsAsync(fileIdentifier, Partition.DefaultName, CancellationToken.None);
        await _metadataStore.Received(1).DeleteInstanceMetadataIfExistsAsync(fileIdentifier, CancellationToken.None);
    }

    [Fact]
    public async Task GivenValidInput_WhenCallingUpdateBlobAsync_ShouldCallUpdateInstanceFileAsync_AndUpdateInstanceMetadataAsync()
    {
        long fileIdentifier = 123;
        long newFileIdentifier = 456;
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(version: fileIdentifier);
        var instanceFileIdentifier = new InstanceFileState { Version = fileIdentifier, NewVersion = newFileIdentifier };
        var datasetToUpdate = new DicomDataset();
        var cancellationToken = CancellationToken.None;

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = RetrieveHelpers.StreamAndStoredFileFromDataset(
            RetrieveHelpers.GenerateDatasetsFromIdentifiers(
                versionedInstanceIdentifiers.First().VersionedInstanceIdentifier),
                _recyclableMemoryStreamManager,
                frames: 3).Result;

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        var binaryData = await BinaryData.FromStreamAsync(copyStream);
        copyStream.Position = 0;

        _fileStore.GetFileAsync(fileIdentifier, Partition.DefaultName, cancellationToken).Returns(streamAndStoredFile.Value);
        _metadataStore.GetInstanceMetadataAsync(fileIdentifier, cancellationToken).Returns(streamAndStoredFile.Key.Dataset);
        _metadataStore.StoreInstanceMetadataAsync(streamAndStoredFile.Key.Dataset, newFileIdentifier, cancellationToken).Returns(Task.CompletedTask);
        _fileStore.GetFileContentInRangeAsync(newFileIdentifier, Partition.Default, Arg.Any<FrameRange>(), cancellationToken).Returns(binaryData);
        _fileStore.UpdateFileBlockAsync(newFileIdentifier, Partition.Default, Arg.Any<string>(), copyStream, cancellationToken).Returns(DefaultFileProperties);

        _fileStore.StoreFileInBlocksAsync(
            newFileIdentifier,
            Partition.Default,
            Arg.Any<Stream>(),
            Arg.Any<IDictionary<string, long>>(),
            cancellationToken)
         .Returns(new Uri("http://contoso.com"));

        FileProperties result = await _updateInstanceService.UpdateInstanceBlobAsync(instanceFileIdentifier, datasetToUpdate, Partition.Default, cancellationToken);

        Assert.Null(result); // file properties should only be passed along when external store is enabled

        await _fileStore.DidNotReceive().CopyFileAsync(fileIdentifier, newFileIdentifier, Partition.Default, cancellationToken);
        await _fileStore.Received(1).GetFileAsync(fileIdentifier, Partition.DefaultName, cancellationToken);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(fileIdentifier, cancellationToken);
        await _metadataStore.Received(1).StoreInstanceMetadataAsync(streamAndStoredFile.Key.Dataset, newFileIdentifier, cancellationToken);
        await _fileStore.Received(1).GetFileContentInRangeAsync(newFileIdentifier, Partition.Default, Arg.Any<FrameRange>(), cancellationToken);
        await _fileStore.Received(1).UpdateFileBlockAsync(newFileIdentifier, Partition.Default, Arg.Any<string>(), Arg.Any<Stream>(), cancellationToken);
        await _fileStore.Received(1).StoreFileInBlocksAsync(
            newFileIdentifier,
            Partition.Default,
            Arg.Any<Stream>(),
            Arg.Is<IDictionary<string, long>>(x => x.Count == 1),
            cancellationToken);

        streamAndStoredFile.Value.Dispose();
        copyStream.Dispose();
    }

    [Fact]
    public async Task GivenValidInputWithLargeFile_WhenCallingUpdateBlobAsync_ShouldCallUpdateInstanceFileAsync_AndUpdateInstanceMetadataAsync()
    {
        long fileIdentifier = 123;
        long newFileIdentifier = 456;
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(version: fileIdentifier);
        var instanceFileIdentifier = new InstanceFileState { Version = fileIdentifier, NewVersion = newFileIdentifier };
        var datasetToUpdate = new DicomDataset();
        var cancellationToken = CancellationToken.None;

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = RetrieveHelpers.StreamAndStoredFileFromDataset(
            RetrieveHelpers.GenerateDatasetsFromIdentifiers(
                versionedInstanceIdentifiers.First().VersionedInstanceIdentifier),
                _recyclableMemoryStreamManager,
                rows: 200,
                columns: 200,
                frames: 100).Result;

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        var binaryData = await BinaryData.FromStreamAsync(copyStream);
        copyStream.Position = 0;

        _fileStore.GetFileAsync(fileIdentifier, Partition.DefaultName, cancellationToken).Returns(streamAndStoredFile.Value);
        _metadataStore.GetInstanceMetadataAsync(fileIdentifier, cancellationToken).Returns(streamAndStoredFile.Key.Dataset);
        _metadataStore.StoreInstanceMetadataAsync(streamAndStoredFile.Key.Dataset, newFileIdentifier, cancellationToken).Returns(Task.CompletedTask);
        _fileStore.GetFileContentInRangeAsync(newFileIdentifier, Partition.Default, Arg.Any<FrameRange>(), cancellationToken).Returns(binaryData);
        _fileStore.UpdateFileBlockAsync(newFileIdentifier, Partition.Default, Arg.Any<string>(), copyStream, cancellationToken).Returns(DefaultFileProperties);

        _fileStore.StoreFileInBlocksAsync(
            newFileIdentifier,
            Partition.Default,
            Arg.Any<Stream>(),
            Arg.Any<IDictionary<string, long>>(),
            cancellationToken)
         .Returns(new Uri("http://contoso.com"));

        await _updateInstanceService.UpdateInstanceBlobAsync(instanceFileIdentifier, datasetToUpdate, Partition.Default, cancellationToken);

        streamAndStoredFile.Key.Dataset.Remove(DicomTag.PixelData);
        var firstBlockLength = await DicomFileExtensions.GetByteLengthAsync(streamAndStoredFile.Key, new RecyclableMemoryStreamManager());
        await _fileStore.DidNotReceive().CopyFileAsync(fileIdentifier, newFileIdentifier, Partition.Default, cancellationToken);
        await _fileStore.Received(1).GetFileAsync(fileIdentifier, Partition.DefaultName, cancellationToken);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(fileIdentifier, cancellationToken);
        await _metadataStore.Received(1).StoreInstanceMetadataAsync(streamAndStoredFile.Key.Dataset, newFileIdentifier, cancellationToken);
        await _fileStore.Received(1).GetFileContentInRangeAsync(newFileIdentifier, Partition.Default, Arg.Is<FrameRange>(x => x.Length == firstBlockLength), cancellationToken);
        await _fileStore.Received(1).UpdateFileBlockAsync(newFileIdentifier, Partition.Default, Arg.Any<string>(), Arg.Any<Stream>(), cancellationToken);
        await _fileStore.Received(1).StoreFileInBlocksAsync(
            newFileIdentifier,
            Partition.Default,
            Arg.Any<Stream>(),
            Arg.Is<IDictionary<string, long>>(x => x.Count == 2 && x.Sum(y => y.Value) == copyStream.Length),
            cancellationToken);

        streamAndStoredFile.Value.Dispose();
        copyStream.Dispose();
    }

    [Fact]
    public async Task GivenValidInputWithExistingFile_WhenCallingUpdateBlobAsync_ShouldCallUpdateInstanceFileAsync_AndUpdateInstanceMetadataAsync()
    {
        long originalFileIdentifier = 123;
        long fileIdentifier = 456;
        long newFileIdentifier = 789;

        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList(version: fileIdentifier);
        var instanceFileIdentifier = new InstanceFileState { Version = fileIdentifier, NewVersion = newFileIdentifier, OriginalVersion = originalFileIdentifier };
        var datasetToUpdate = new DicomDataset();
        var cancellationToken = CancellationToken.None;

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = RetrieveHelpers.StreamAndStoredFileFromDataset(
            RetrieveHelpers.GenerateDatasetsFromIdentifiers(
                versionedInstanceIdentifiers.First().VersionedInstanceIdentifier),
                _recyclableMemoryStreamManager,
                rows: 200,
                columns: 200,
                frames: 100).Result;
        var firstBlockLength = await DicomFileExtensions.GetByteLengthAsync(streamAndStoredFile.Key, new RecyclableMemoryStreamManager());

        MemoryStream copyStream = _recyclableMemoryStreamManager.GetStream();
        await streamAndStoredFile.Value.CopyToAsync(copyStream);
        copyStream.Position = 0;
        streamAndStoredFile.Value.Position = 0;

        var binaryData = await BinaryData.FromStreamAsync(copyStream);
        copyStream.Position = 0;

        byte[] buffer = new byte[firstBlockLength];
        await copyStream.ReadAsync(buffer, 0, buffer.Length);
        var stream = new MemoryStream(buffer);

        var firstBlock = new KeyValuePair<string, long>(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), stream.Length);

        _fileStore.CopyFileAsync(fileIdentifier, newFileIdentifier, Partition.Default, cancellationToken).Returns(Task.CompletedTask);
        _fileStore.GetFileAsync(fileIdentifier, Partition.DefaultName, cancellationToken).Returns(streamAndStoredFile.Value);
        _metadataStore.GetInstanceMetadataAsync(fileIdentifier, cancellationToken).Returns(streamAndStoredFile.Key.Dataset);
        _metadataStore.StoreInstanceMetadataAsync(streamAndStoredFile.Key.Dataset, newFileIdentifier, cancellationToken).Returns(Task.CompletedTask);
        _fileStore.GetFileContentInRangeAsync(newFileIdentifier, Partition.Default, Arg.Any<FrameRange>(), cancellationToken).Returns(binaryData);
        _fileStore.UpdateFileBlockAsync(newFileIdentifier, Partition.Default, Arg.Any<string>(), copyStream, cancellationToken).Returns(DefaultFileProperties);
        _fileStore.GetFirstBlockPropertyAsync(newFileIdentifier, Partition.Default, cancellationToken).Returns(firstBlock);

        _fileStore.StoreFileInBlocksAsync(
            newFileIdentifier,
            Partition.Default,
            Arg.Any<Stream>(),
            Arg.Any<IDictionary<string, long>>(),
            cancellationToken)
         .Returns(new Uri("http://contoso.com"));

        await _updateInstanceService.UpdateInstanceBlobAsync(instanceFileIdentifier, datasetToUpdate, Partition.Default, cancellationToken);

        streamAndStoredFile.Key.Dataset.Remove(DicomTag.PixelData);

        await _fileStore.Received(1).CopyFileAsync(fileIdentifier, newFileIdentifier, Partition.Default, cancellationToken);
        await _fileStore.DidNotReceive().GetFileAsync(fileIdentifier, Partition.DefaultName, cancellationToken);
        await _metadataStore.Received(1).GetInstanceMetadataAsync(fileIdentifier, cancellationToken);
        await _metadataStore.Received(1).StoreInstanceMetadataAsync(streamAndStoredFile.Key.Dataset, newFileIdentifier, cancellationToken);
        await _fileStore.Received(1).GetFileContentInRangeAsync(newFileIdentifier, Partition.Default, Arg.Is<FrameRange>(x => x.Length == firstBlockLength), cancellationToken);
        await _fileStore.Received(1).UpdateFileBlockAsync(newFileIdentifier, Partition.Default, Arg.Any<string>(), Arg.Any<Stream>(), cancellationToken);
        await _fileStore.DidNotReceive().StoreFileInBlocksAsync(
            newFileIdentifier,
            Partition.Default,
            Arg.Any<Stream>(),
            Arg.Is<IDictionary<string, long>>(x => x.Count == 2 && x.Sum(y => y.Value) == copyStream.Length),
            cancellationToken);
        await _fileStore.Received(1).GetFirstBlockPropertyAsync(newFileIdentifier, Partition.Default, cancellationToken);

        streamAndStoredFile.Value.Dispose();
        copyStream.Dispose();
    }

    private static List<InstanceMetadata> SetupInstanceIdentifiersList(long version, Partition partition = null, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();
        instanceProperty ??= new InstanceProperties();
        partition ??= Partition.Default;
        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), version, partition), instanceProperty));
        return dicomInstanceIdentifiersList;
    }
}
