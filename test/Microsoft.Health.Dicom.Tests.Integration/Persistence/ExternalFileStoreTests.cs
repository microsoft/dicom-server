// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Update;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class ExternalFileStoreTests : IClassFixture<DataStoreTestsFixture>
{
    private readonly IFileStore _blobDataStore;
    private readonly Func<int> _getNextWatermark;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private static string ConditionNotMetMessage => "Received the following error code: ConditionNotMet";
    private static string SourceConditionNotMetMessage => "Received the following error code: SourceConditionNotMet";
    private readonly bool _isDevEnv;
    private readonly FileProperties _defaultFileProperties = new FileProperties
    {
        Path = "partitionA/123.dcm",
        ETag = "e45678"
    };

    public ExternalFileStoreTests(DataStoreTestsFixture fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _blobDataStore = fixture.ExternalFileStore;
        _getNextWatermark = () => fixture.NextWatermark;
        _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        _isDevEnv = fixture.IsDevEnv;
    }

    [Fact]
    public async Task GivenAValidFileStream_WhenStored_ThenItCanBeRetrievedAndDeleted()
    {
        var version = _getNextWatermark();

        var fileData = new byte[] { 4, 7, 2 };

        // Store the file.
        FileProperties fileProperties = await AddFileAsync(version, fileData, $"{nameof
        (GivenAValidFileStream_WhenStored_ThenItCanBeRetrievedAndDeleted)}.fileData");

        Assert.NotNull(fileProperties);

        // Should be able to retrieve.
        await using (Stream resultStream = await _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties))
        {
            Assert.Equal(
                fileData,
                await ConvertStreamToByteArrayAsync(resultStream));
        }

        // Should be able to delete.
        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties);

        // The file should no longer exists.
        await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties));
    }

    [Fact]
    public async Task GivenFileAlreadyExists_WhenStored_ThenExistingFileWillBeOverwrittenWithDifferentETag()
    {
        var version = _getNextWatermark();

        var fileData1 = new byte[] { 4, 7, 2 };

        FileProperties fileProperties1 = await AddFileAsync(version, fileData1, "fileDataTag");

        var fileData2 = new byte[] { 1, 3, 5 };

        FileProperties fileProperties2 = await AddFileAsync(version, fileData2, "fileDataTag");

        Assert.Equal(fileProperties1.Path, fileProperties2.Path);
        // while the path may be the same, the eTag is expected to be different on file rewrites
        Assert.NotEqual(fileProperties1.ETag, fileProperties2.ETag);

        await using (Stream resultStream = await _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties2))
        {
            Assert.Equal(
                fileData2,
                await ConvertStreamToByteArrayAsync(resultStream));
        }

        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties2);
    }

    [Fact]
    public async Task GivenFileAlreadyExists_WhenDeletedAndThenRestored_ThenExistingFileWillBeRewrittenWithDifferentETag()
    {
        // Note that modifying metadata also changes the etag of the blob
        var version = _getNextWatermark();

        // store the file
        var fileData1 = new byte[] { 4, 7, 2 };
        FileProperties fileProperties1 = await AddFileAsync(version, fileData1, "fileDataTag");

        // file is deleted
        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties1);
        await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties1));

        // store file again with same path
        var fileData2 = new byte[] { 1, 3, 5 };
        FileProperties fileProperties2 = await AddFileAsync(version, fileData2, "fileDataTag");

        // expect file path same
        Assert.Equal(fileProperties1.Path, fileProperties2.Path);
        // while the path may be the same, the eTag is expected to be different on file rewrites
        Assert.NotEqual(fileProperties1.ETag, fileProperties2.ETag);
        // assert that content is the same
        await using (Stream resultStream = await _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties2))
        {
            Assert.Equal(
                fileData2,
                await ConvertStreamToByteArrayAsync(resultStream));
        }

        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties2);
    }

    [Fact]
    public async Task GivenFileWithETag_WhenOperatingOnFileWithDifferentETagForCondition_ThenExpectExceptions()
    {
        // Note that modifying metadata also changes the etag of the blob
        var version = _getNextWatermark();

        // store the file with committed blocks
        FileProperties fileProperties = await AddFileInBlocksAsync(version, new byte[] { 4, 7, 2 }, "fileDataTag");

        FileProperties badFileProperties = new FileProperties { Path = fileProperties.Path, ETag = "badETag" };

        Assert.NotEqual(badFileProperties.ETag, fileProperties.ETag);

        var getFileEx = await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => _blobDataStore.GetFileAsync(version, Partition.Default, badFileProperties));
        Assert.Contains(ConditionNotMetMessage, getFileEx.Message);

        var copyFileEx = await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => _blobDataStore.CopyFileAsync(version, _getNextWatermark(), Partition.Default, badFileProperties));
        Assert.Contains(ExpectedFailedSubstring(), copyFileEx.Message);

        var getFilePropsEx = await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => _blobDataStore.GetFilePropertiesAsync(version, Partition.Default, badFileProperties));
        Assert.Contains(ConditionNotMetMessage, getFilePropsEx.Message);
    }

    [Fact]
    public async Task GivenFileWithETag_WhenDeletingFileWithDifferentETagForCondition_ThenExpectNoExceptions()
    {
        // Note that modifying metadata also changes the etag of the blob
        var version = _getNextWatermark();

        // store the file with committed blocks
        FileProperties fileProperties = await AddFileInBlocksAsync(version, new byte[] { 4, 7, 2 }, "fileDataTag");

        FileProperties badFileProperties = new FileProperties { Path = fileProperties.Path, ETag = "badETag" };

        Assert.NotEqual(badFileProperties.ETag, fileProperties.ETag);

        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, badFileProperties);
    }

    [Fact]
    public async Task GivenANonExistentFile_WhenRetrieving_ThenDataStoreRequestFailedExceptionShouldBeThrown()
    {
        await Assert.ThrowsAsync<DataStoreRequestFailedException>(() => _blobDataStore.GetFileAsync(_getNextWatermark(), Partition.Default, new FileProperties()));
    }

    [Fact]
    public async Task GivenANonExistentFile_WhenDeletingWithFileProperties_ThenItShouldNotThrowException()
    {
        await _blobDataStore.DeleteFileIfExistsAsync(_getNextWatermark(), Partition.Default, _defaultFileProperties);
    }

    private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
    {
        await using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
        {
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private async Task<FileProperties> AddFileAsync(long version, byte[] bytes, string tag, CancellationToken cancellationToken = default)
    {
        await using (var stream = _recyclableMemoryStreamManager.GetStream(tag, bytes, 0, bytes.Length))
        {
            return await _blobDataStore.StoreFileAsync(version, Partition.DefaultName, stream, cancellationToken);
        }
    }

    private async Task<FileProperties> AddFileInBlocksAsync(long version, byte[] bytes, string tag, CancellationToken cancellationToken = default)
    {
        await using (var stream = _recyclableMemoryStreamManager.GetStream(tag, bytes, 0, bytes.Length))
        {
            return await _blobDataStore.StoreFileInBlocksAsync(version, Partition.Default, stream, UpdateInstanceService.GetBlockLengths(stream.Length, stream.Length, (int)stream.Length), cancellationToken);
        }
    }

    private string ExpectedFailedSubstring()
    {
        return _isDevEnv ? ConditionNotMetMessage : SourceConditionNotMetMessage;
    }
}
