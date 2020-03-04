// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomBlobStorageTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomBlobDataStore _dicomBlobDataStore;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public DicomBlobStorageTests(DicomBlobStorageTestsFixture fixture)
        {
            _dicomBlobDataStore = fixture.DicomBlobDataStore;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task WhenStoringBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.AddFileAsStreamAsync(string.Empty, stream));
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.AddFileAsStreamAsync(new string('c', 1025), stream));
                await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomBlobDataStore.AddFileAsStreamAsync("validname", null));
            }
        }

        [Fact]
        public async Task WhenFetchingBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(new string('c', 1025)));
        }

        [Fact]
        public async Task WhenDeletingBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.DeleteFileIfExistsAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.DeleteFileIfExistsAsync(new string('c', 1025)));
        }

        [Fact]
        public async Task GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileData = new byte[] { 4, 7, 2 };

            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream("GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted.fileData", fileData, 0, fileData.Length))
            {
                Uri fileLocation = await _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream);

                Assert.NotNull(fileLocation);
                Assert.EndsWith(fileName, fileLocation.AbsoluteUri);
            }

            await using (Stream resultStream = await _dicomBlobDataStore.GetFileAsStreamAsync(fileName))
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

            await using (var stream = _recyclableMemoryStreamManager.GetStream("GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists.fileData", fileData, 0, fileData.Length))
            {
                // Overwrite test
                Uri fileLocation1 = await _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream);
                Uri fileLocation2 = await _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream, overwriteIfExists: true);
                Assert.Equal(fileLocation1, fileLocation2);

                // Fail on exists
                DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                                    () => _dicomBlobDataStore.AddFileAsStreamAsync(fileName, stream, overwriteIfExists: false));
                Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
            }

            await _dicomBlobDataStore.DeleteFileIfExistsAsync(fileName);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenFetched_ThrowsNotFoundException()
        {
            var fileName = Guid.NewGuid().ToString();
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(fileName));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenDeleted_DoesNotThrowException()
        {
            var fileName = Guid.NewGuid().ToString();
            await _dicomBlobDataStore.DeleteFileIfExistsAsync(fileName);
        }

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            await using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
