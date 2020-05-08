// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class FileStoreTests : IClassFixture<DataStoreTestsFixture>
    {
        private readonly IFileStore _blobDataStore;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public FileStoreTests(DataStoreTestsFixture fixture)
        {
            _blobDataStore = fixture.FileStore;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenAValidFileStream_WhenStored_ThenItCanBeRetrievedAndDeleted()
        {
            VersionedInstanceIdentifier instanceIdentifier = GenerateIdentifier();

            var fileData = new byte[] { 4, 7, 2 };

            // Store the file.
            Uri fileLocation = await AddFileAsync(instanceIdentifier, fileData, $"{nameof(GivenAValidFileStream_WhenStored_ThenItCanBeRetrievedAndDeleted)}.fileData");

            Assert.NotNull(fileLocation);

            // Should be able to retrieve.
            await using (Stream resultStream = await _blobDataStore.GetFileAsync(instanceIdentifier))
            {
                Assert.Equal(
                    fileData,
                    await ConvertStreamToByteArrayAsync(resultStream));
            }

            // Should be able to delete.
            await _blobDataStore.DeleteFileIfExistsAsync(instanceIdentifier);

            // The file should no longer exists.
            await Assert.ThrowsAsync<ItemNotFoundException>(() => _blobDataStore.GetFileAsync(instanceIdentifier));
        }

        [Fact]
        public async Task GivenFileAlreadyExists_WhenStored_ThenExistingFileWillBeOverwritten()
        {
            VersionedInstanceIdentifier instanceIdentifier = GenerateIdentifier();

            var fileData1 = new byte[] { 4, 7, 2 };

            Uri fileLocation1 = await AddFileAsync(instanceIdentifier, fileData1, $"{nameof(GivenFileAlreadyExists_WhenStored_ThenExistingFileWillBeOverwritten)}.fileData1");

            var fileData2 = new byte[] { 1, 3, 5 };

            Uri fileLocation2 = await AddFileAsync(instanceIdentifier, fileData2, $"{nameof(GivenFileAlreadyExists_WhenStored_ThenExistingFileWillBeOverwritten)}.fileData2");

            Assert.Equal(fileLocation1, fileLocation2);

            await using (Stream resultStream = await _blobDataStore.GetFileAsync(instanceIdentifier))
            {
                Assert.Equal(
                    fileData2,
                    await ConvertStreamToByteArrayAsync(resultStream));
            }

            await _blobDataStore.DeleteFileIfExistsAsync(instanceIdentifier);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenRetrieving_ThenItemNotFoundExceptionShouldBeThrown()
        {
            VersionedInstanceIdentifier instanceIdentifier = GenerateIdentifier();

            await Assert.ThrowsAsync<ItemNotFoundException>(() => _blobDataStore.GetFileAsync(instanceIdentifier));
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenDeleting_ThenItShouldNotThrowException()
        {
            VersionedInstanceIdentifier instanceIdentifier = GenerateIdentifier();

            await _blobDataStore.DeleteFileIfExistsAsync(instanceIdentifier);
        }

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            await using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static VersionedInstanceIdentifier GenerateIdentifier()
            => new VersionedInstanceIdentifier(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                version: 0);

        private async Task<Uri> AddFileAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, byte[] bytes, string tag, CancellationToken cancellationToken = default)
        {
            await using (var stream = _recyclableMemoryStreamManager.GetStream(tag, bytes, 0, bytes.Length))
            {
                return await _blobDataStore.StoreFileAsync(versionedInstanceIdentifier, stream, cancellationToken);
            }
        }
    }
}
