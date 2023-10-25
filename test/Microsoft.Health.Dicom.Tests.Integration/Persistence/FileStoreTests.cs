// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class FileStoreTests : IClassFixture<DataStoreTestsFixture>
{
    private readonly IFileStore _blobDataStore;
    private readonly Func<int> _getNextWatermark;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly BlobContainerClient _containerClient;

    public FileStoreTests(DataStoreTestsFixture fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        _blobDataStore = fixture.FileStore;
        _getNextWatermark = () => fixture.NextWatermark;
        _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        _containerClient = fixture.GetBlobContainerClient();
    }

    [Fact]
    public async Task GivenAValidFileStream_WhenStored_ThenItCanBeRetrievedAndDeleted()
    {
        var version = _getNextWatermark();

        var fileData = new byte[] { 4, 7, 2 };

        // Store the file.
        await AddFileAsync(version, fileData, $"{nameof(GivenAValidFileStream_WhenStored_ThenItCanBeRetrievedAndDeleted)}.fileData");

        // Should be able to retrieve.
        await using (Stream resultStream = await _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties: null))
        {
            Assert.Equal(
                fileData,
                await ConvertStreamToByteArrayAsync(resultStream));
        }

        // Should be able to delete.
        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties: null);

        // The file should no longer exists.
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties: null));
    }

    [Fact]
    public async Task GivenFileAlreadyExists_WhenStored_ThenExistingFileWillBeOverwritten()
    {
        var version = _getNextWatermark();

        var fileData1 = new byte[] { 4, 7, 2 };

        await AddFileAsync(version, fileData1, "fileDataTag");

        var fileData2 = new byte[] { 1, 3, 5 };

        Assert.NotNull(await AddFileAsync(version, fileData2, "fileDataTag"));

        await using (Stream resultStream = await _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties: null))
        {
            Assert.Equal(
                fileData2,
                await ConvertStreamToByteArrayAsync(resultStream));
        }

        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties: null);
    }

    [Fact]
    public async Task GivenFileAlreadyExists_WhenDeletedAndThenRestored_ThenExistingFileWillBeRewritten()
    {
        var version = _getNextWatermark();

        // store the file
        var fileData1 = new byte[] { 4, 7, 2 };
        await AddFileAsync(version, fileData1, "fileDataTag");

        // file is deleted
        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties: null);
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties: null));

        // store file again with same path
        var fileData2 = new byte[] { 1, 3, 5 };
        Assert.NotNull(await AddFileAsync(version, fileData2, "fileDataTag"));

        // assert that content is the same
        await using (Stream resultStream = await _blobDataStore.GetFileAsync(version, Partition.Default, fileProperties: null))
        {
            Assert.Equal(
                fileData2,
                await ConvertStreamToByteArrayAsync(resultStream));
        }

        await _blobDataStore.DeleteFileIfExistsAsync(version, Partition.Default, fileProperties: null);
    }

    [Fact]
    public async Task GivenANonExistentFile_WhenRetrieving_ThenItemNotFoundExceptionShouldBeThrown()
    {
        await Assert.ThrowsAsync<ItemNotFoundException>(() => _blobDataStore.GetFileAsync(_getNextWatermark(), Partition.Default, fileProperties: null));
    }

    [Fact]
    public async Task GivenANonExistentFile_WhenDeleting_ThenItShouldNotThrowException()
    {
        await _blobDataStore.DeleteFileIfExistsAsync(_getNextWatermark(), Partition.Default, fileProperties: null);
    }

    [Fact]
    public async Task GivenFileAndStored_WhenAccessTierChanged_ThenTierIsSetCorrectly()
    {
        var version = _getNextWatermark();

        var fileData = new byte[] { 4, 7, 2 };

        // Store the file.
        await AddFileAsync(version, fileData, $"{nameof(GivenFileAndStored_WhenAccessTierChanged_ThenTierIsSetCorrectly)}.fileData");

        var properties = await _blobDataStore.GetFilePropertiesAsync(version, Partition.DefaultName);

        var blockBlobClient = _containerClient.GetBlockBlobClient(properties.Path);

        var fullProperties = await blockBlobClient.GetPropertiesAsync();

        // Verify before setting to cold tier, the access tier is hot.
        Assert.Equal(AccessTier.Hot, fullProperties.Value.AccessTier);

        await _blobDataStore.SetBlobToColdAccessTierAsync(version, Partition.Default, fileProperties: null);

        fullProperties = await blockBlobClient.GetPropertiesAsync();

        Assert.Equal(AccessTier.Cold, fullProperties.Value.AccessTier);
    }

    private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
    {
        await using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
        {
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private async Task<FileProperties> AddFileAsync(long version, byte[] bytes, string tag, CancellationToken cancellationToken =
     default)
    {
        await using (var stream = _recyclableMemoryStreamManager.GetStream(tag, bytes, 0, bytes.Length))
        {
            return await _blobDataStore.StoreFileAsync(version, Partition.DefaultName, stream, cancellationToken);
        }
    }
}
