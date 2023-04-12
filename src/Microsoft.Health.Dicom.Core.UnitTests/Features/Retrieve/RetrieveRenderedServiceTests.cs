// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom.Imaging;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using NSubstitute;
using Xunit;
using SixLabors.ImageSharp;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve;
public class RetrieveRenderedServiceTests
{
    private readonly RetrieveRenderedService _retrieveRenderedService;
    private readonly IInstanceStore _instanceStore;
    private readonly IFileStore _fileStore;
    private readonly IDicomRequestContextAccessor _dicomRequestContextAccessor;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<RetrieveRenderedService> _logger;

    private readonly string _studyInstanceUid = TestUidGenerator.Generate();
    private readonly string _firstSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _secondSeriesInstanceUid = TestUidGenerator.Generate();
    private readonly string _sopInstanceUid = TestUidGenerator.Generate();
    private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;


    public RetrieveRenderedServiceTests()
    {
        _instanceStore = Substitute.For<IInstanceStore>();
        _fileStore = Substitute.For<IFileStore>();
        _logger = NullLogger<RetrieveRenderedService>.Instance;
        _dicomRequestContextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _dicomRequestContextAccessor.RequestContext.DataPartitionEntry = PartitionEntry.Default;
        var retrieveConfigurationSnapshot = Substitute.For<IOptionsSnapshot<RetrieveConfiguration>>();
        retrieveConfigurationSnapshot.Value.Returns(new RetrieveConfiguration());
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _retrieveRenderedService = new RetrieveRenderedService(
            _instanceStore,
            _fileStore,
            _dicomRequestContextAccessor,
            retrieveConfigurationSnapshot,
            _recyclableMemoryStreamManager,
            _logger
            );

        new DicomSetupBuilder()
        .RegisterServices(s => s.AddImageManager<ImageSharpImageManager>())
        .Build();
    }

    [Fact]
    public async Task GivenARequestWithMultipleAcceptHeaders_WhenHandlerIsExecuted_ThenNotAcceptableExceptionExceptionIsThrown()
    {
        const string expectedErrorMessage = "The request contains multiple accept headers, which is not supported.";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader(), AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
        var ex = await Assert.ThrowsAsync<NotAcceptableException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task GivenARequestWithInvalidAcceptHeader_WhenHandlerIsExecuted_ThenNotAcceptableExceptionExceptionIsThrown()
    {
        const string expectedErrorMessage = "The request headers are not acceptable";

        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        RetrieveRenderedRequest request = new RetrieveRenderedRequest(studyInstanceUid, seriesInstanceUid, sopInstanceUid, ResourceType.Frames, 5, new[] { AcceptHeaderHelpers.CreateAcceptHeaderForGetStudy() });
        var ex = await Assert.ThrowsAsync<NotAcceptableException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(request, CancellationToken.None));
        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task GivenNoStoredInstances_RenderForInstance_ThenNotFoundIsThrown()
    {
        _instanceStore.GetInstanceIdentifiersInStudyAsync(DefaultPartition.Key, _studyInstanceUid).Returns(new List<VersionedInstanceIdentifier>());

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(
            new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Instance, 0, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() }),
            DefaultCancellationToken));
    }

    [Fact]
    public async Task GivenFileSizeTooLarge_RenderForInstance_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList();
        long aboveMaxFileSize = new RetrieveConfiguration().MaxDicomFileSize + 1;
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = aboveMaxFileSize });

        await Assert.ThrowsAsync<NotAcceptableException>(() =>
        _retrieveRenderedService.RetrieveRenderedImageAsync(
              new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 0, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() }),
              DefaultCancellationToken));

    }

    [Fact]
    public async Task GivenStoredInstancesWithFrames_WhenRenderRequestForNonExistingFrame_ThenNotFoundIsThrown()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList();

        // For the instance, set up the fileStore to return a stream containing the file associated with the identifier with 3 frames.
        Stream streamOfStoredFiles = RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3).Result.Value;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamOfStoredFiles);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamOfStoredFiles.Length });

        var retrieveRenderRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 4, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });

        await Assert.ThrowsAsync<FrameNotFoundException>(() => _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderRequest,
               DefaultCancellationToken));

        // Dispose the stream.
        streamOfStoredFiles.Dispose();
    }

    [Fact]
    public async Task GivenStoredInstancesWithFrames_WhenRetrieveRenderedForFrames_ThenEachFrameRenderedSuccesfully()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList();

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3).Result;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamAndStoredFile.Value.Length });

        var retrieveRenderedRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 0, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });

        RetrieveRenderedResponse response = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest,
               DefaultCancellationToken);

        streamAndStoredFile.Value.Position = 0;
        DicomFile dicomFile = await DicomFile.OpenAsync(streamAndStoredFile.Value, FileReadOption.ReadLargeOnDemand);
        DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
        using var img = dicomImage.RenderImage(0);
        using var sharpImage = img.AsSharpImage();
        MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await sharpImage.SaveAsJpegAsync(resultStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), DefaultCancellationToken);
        resultStream.Position = 0;
        AssertStreamsEqual(resultStream, response.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);

        var retrieveRenderedRequest2 = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Frames, 1, new[] { AcceptHeaderHelpers.CreateRenderJpegAcceptHeader() });
        streamAndStoredFile.Value.Position = 0;

        RetrieveRenderedResponse response2 = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest2,
               DefaultCancellationToken);

        streamAndStoredFile.Value.Position = 0;
        using var img2 = dicomImage.RenderImage(1);
        using var sharpImage2 = img2.AsSharpImage();
        MemoryStream resultStream2 = _recyclableMemoryStreamManager.GetStream();
        await sharpImage2.SaveAsJpegAsync(resultStream2, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), DefaultCancellationToken);
        resultStream2.Position = 0;
        AssertStreamsEqual(resultStream2, response2.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);

        streamAndStoredFile.Value.Dispose();

    }

    [Fact]
    public async Task GivenStoredInstances_WhenRetrieveRenderedWithoutSpecifyingAcceptHeaders_ThenRenderJpegSuccesfully()
    {
        List<InstanceMetadata> versionedInstanceIdentifiers = SetupInstanceIdentifiersList();

        KeyValuePair<DicomFile, Stream> streamAndStoredFile = RetrieveHelpers.StreamAndStoredFileFromDataset(RetrieveHelpers.GenerateDatasetsFromIdentifiers(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier), _recyclableMemoryStreamManager, frames: 3).Result;
        _fileStore.GetFileAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(streamAndStoredFile.Value);
        _fileStore.GetFilePropertiesAsync(versionedInstanceIdentifiers.First().VersionedInstanceIdentifier, DefaultCancellationToken).Returns(new FileProperties() { ContentLength = streamAndStoredFile.Value.Length });


        var retrieveRenderedRequest = new RetrieveRenderedRequest(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, ResourceType.Instance, 0, new List<AcceptHeader>());

        RetrieveRenderedResponse response = await _retrieveRenderedService.RetrieveRenderedImageAsync(
               retrieveRenderedRequest,
               DefaultCancellationToken);

        streamAndStoredFile.Value.Position = 0;
        DicomFile dicomFile = await DicomFile.OpenAsync(streamAndStoredFile.Value, FileReadOption.ReadLargeOnDemand);
        DicomImage dicomImage = new DicomImage(dicomFile.Dataset);
        using var img = dicomImage.RenderImage(0);
        using var sharpImage = img.AsSharpImage();
        MemoryStream resultStream = _recyclableMemoryStreamManager.GetStream();
        await sharpImage.SaveAsJpegAsync(resultStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), DefaultCancellationToken);
        resultStream.Position = 0;
        AssertStreamsEqual(resultStream, response.ResponseStream);
        Assert.Equal("image/jpeg", response.ContentType);

        streamAndStoredFile.Value.Dispose();

    }

    private List<InstanceMetadata> SetupInstanceIdentifiersList(int partitionKey = DefaultPartition.Key, InstanceProperties instanceProperty = null)
    {
        var dicomInstanceIdentifiersList = new List<InstanceMetadata>();

        instanceProperty = instanceProperty ?? new InstanceProperties();

        dicomInstanceIdentifiersList.Add(new InstanceMetadata(new VersionedInstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, TestUidGenerator.Generate(), 0, partitionKey), instanceProperty));
        _instanceStore.GetInstanceIdentifierWithPropertiesAsync(partitionKey, _studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, DefaultCancellationToken).Returns(dicomInstanceIdentifiersList);
        var identifier = new InstanceIdentifier(_studyInstanceUid, _firstSeriesInstanceUid, _sopInstanceUid, partitionKey);

        return dicomInstanceIdentifiersList;
    }

    private static void AssertStreamsEqual(Stream expectedPixelData, Stream actualPixelData)
    {
        Assert.Equal(expectedPixelData.Length, actualPixelData.Length);
        Assert.Equal(0, actualPixelData.Position);
        Assert.Equal(0, expectedPixelData.Position);
        for (var i = 0; i < expectedPixelData.Length; i++)
        {
            Assert.Equal(expectedPixelData.ReadByte(), actualPixelData.ReadByte());
        }
    }
}
