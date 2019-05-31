// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Xunit;

namespace Microsoft.Health.DicomTests.Integration.Persistence
{
    public class DicomBlobStorageTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomBlobDataStore _dicomBlobDataStore;

        public DicomBlobStorageTests(DicomBlobStorageTestsFixture fixture)
        {
            _dicomBlobDataStore = fixture.DicomBlobDataStore;
        }

        [Fact]
        public async Task WhenStoringBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            using (var stream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.AddFileAsStreamAsync(string.Empty, stream));
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.AddFileAsStreamAsync(new string('c', 1025), stream));
                await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomBlobDataStore.AddFileAsStreamAsync("validname", null));
            }
        }

        [Fact]
        public async Task WhenFetchingBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            using (var stream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(string.Empty));
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(new string('c', 1025)));
            }
        }

        [Fact]
        public async Task WhenDeletingBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            using (var stream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.DeleteFileIfExistsAsync(string.Empty));
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.DeleteFileIfExistsAsync(new string('c', 1025)));
            }
        }

        [Fact]
        public async Task GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileData = new byte[] { 4, 7, 2 };

            using (var stream = new MemoryStream(fileData))
            {
                Uri fileLocation = await _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream);

                Assert.NotNull(fileLocation);
                Assert.EndsWith(fileName, fileLocation.AbsoluteUri);
            }

            using (Stream resultStream = await _dicomBlobDataStore.GetFileAsStreamAsync(fileName))
            {
                byte[] result = await ConvertStreamToByteArrayAsync(resultStream);
                Assert.Equal(fileData, result);
            }

            await _dicomBlobDataStore.DeleteFileIfExistsAsync(fileName);
        }

        [Fact]
        public async Task GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileData = new byte[] { 4, 7, 2 };

            using (var stream = new MemoryStream(fileData))
            {
                // Overwrite test
                Uri fileLocation1 = await _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream);
                Uri fileLocation2 = await _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream, overwriteIfExists: true);
                Assert.Equal(fileLocation1, fileLocation2);

                // Fail on exists
                StorageException exception = await Assert.ThrowsAsync<StorageException>(() => _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream, overwriteIfExists: false));
                Assert.Equal((int)HttpStatusCode.Conflict, exception.RequestInformation.HttpStatusCode);
            }

            await _dicomBlobDataStore.DeleteFileIfExistsAsync(fileName);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenFetched_ThrowsNotFoundException()
        {
            var fileName = Guid.NewGuid().ToString();
            StorageException exception = await Assert.ThrowsAsync<StorageException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(fileName));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.RequestInformation.HttpStatusCode);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenDeleted_DoesNotThrowException()
        {
            var fileName = Guid.NewGuid().ToString();
            await _dicomBlobDataStore.DeleteFileIfExistsAsync(fileName);
        }

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
