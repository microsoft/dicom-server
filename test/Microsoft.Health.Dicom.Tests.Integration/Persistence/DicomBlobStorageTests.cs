// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
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
                await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomBlobDataStore.AddInstanceAsStreamAsync(null, stream));
                await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomBlobDataStore.AddInstanceAsStreamAsync(
                    new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID),
                    null));
            }
        }

        [Fact]
        public async Task WhenFetchingBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            using (var stream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => _dicomBlobDataStore.GetInstanceAsStreamAsync(null));
            }
        }

        [Fact]
        public async Task WhenDeletingBlobWithInvalidParameters_ArgumentExceptionIsThrown()
        {
            using (var stream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentException>(() => _dicomBlobDataStore.DeleteInstanceIfExistsAsync(null));
            }
        }

        [Fact]
        public async Task GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted()
        {
            var dicomInstance = new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            var fileData = new byte[] { 4, 7, 2 };

            using (var stream = new MemoryStream(fileData))
            {
                Uri fileLocation = await _dicomBlobDataStore.AddInstanceAsStreamAsync(dicomInstance, stream);
                Assert.NotNull(fileLocation);
            }

            using (Stream resultStream = await _dicomBlobDataStore.GetInstanceAsStreamAsync(dicomInstance))
            {
                byte[] result = await ConvertStreamToByteArrayAsync(resultStream);
                Assert.Equal(fileData, result);
            }

            await _dicomBlobDataStore.DeleteInstanceIfExistsAsync(dicomInstance);
        }

        [Fact]
        public async Task GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists()
        {
            var dicomInstance = new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            var fileData = new byte[] { 4, 7, 2 };

            using (var stream = new MemoryStream(fileData))
            {
                // Overwrite test
                Uri fileLocation1 = await _dicomBlobDataStore.AddInstanceAsStreamAsync(dicomInstance, stream);
                Uri fileLocation2 = await _dicomBlobDataStore.AddInstanceAsStreamAsync(dicomInstance, stream, overwriteIfExists: true);
                Assert.Equal(fileLocation1, fileLocation2);

                // Fail on exists
                DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                                    () => _dicomBlobDataStore.AddInstanceAsStreamAsync(dicomInstance, stream, overwriteIfExists: false));
                Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
            }

            await _dicomBlobDataStore.DeleteInstanceIfExistsAsync(dicomInstance);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenFetched_ThrowsNotFoundException()
        {
            var dicomInstance = new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(() => _dicomBlobDataStore.GetInstanceAsStreamAsync(dicomInstance));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenDeleted_DoesNotThrowException()
        {
            var dicomInstance = new DicomInstance(DicomUID.Generate().UID, DicomUID.Generate().UID, DicomUID.Generate().UID);
            await _dicomBlobDataStore.DeleteInstanceIfExistsAsync(dicomInstance);
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
