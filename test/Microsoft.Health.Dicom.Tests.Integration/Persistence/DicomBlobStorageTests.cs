// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Persistence.Exceptions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomBlobStorageTests : IClassFixture<DicomBlobStorageTestsFixture>
    {
        private readonly IDicomFileStore _dicomBlobDataStore;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public DicomBlobStorageTests(DicomBlobStorageTestsFixture fixture)
        {
            _dicomBlobDataStore = fixture.DicomBlobDataStore;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted()
        {
            var id = new DicomInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 0);

            var fileName = DicomFileBlobStore.GetBlobStorageName(id);
            var fileData = new byte[] { 4, 7, 2 };

            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream("GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted.fileData", fileData, 0, fileData.Length))
            {
                Uri fileLocation = await _dicomBlobDataStore.AddFileAsStreamAsync(id, stream);

                Assert.NotNull(fileLocation);
                Assert.EndsWith(fileName, fileLocation.AbsoluteUri);
            }

            await using (Stream resultStream = await _dicomBlobDataStore.GetFileAsStreamAsync(id))
            {
                byte[] result = await ConvertStreamToByteArrayAsync(resultStream);
                Assert.Equal(fileData, result);
            }

            await _dicomBlobDataStore.DeleteFileIfExistsAsync(id);
        }

        [Fact]
        public async Task GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists()
        {
            var id = new DicomInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 0);
            var fileData = new byte[] { 4, 7, 2 };

            await using (var stream = _recyclableMemoryStreamManager.GetStream("GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists.fileData", fileData, 0, fileData.Length))
            {
                // Overwrite test
                Uri fileLocation1 = await _dicomBlobDataStore.AddFileAsStreamAsync(id, stream);
                Uri fileLocation2 = await _dicomBlobDataStore.AddFileAsStreamAsync(id, stream, overwriteIfExists: true);
                Assert.Equal(fileLocation1, fileLocation2);

                // Fail on exists
                DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(
                                    () => _dicomBlobDataStore.AddFileAsStreamAsync(id, stream, overwriteIfExists: false));
                Assert.Equal((int)HttpStatusCode.Conflict, exception.StatusCode);
            }

            await _dicomBlobDataStore.DeleteFileIfExistsAsync(id);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenFetched_ThrowsNotFoundException()
        {
            var id = new DicomInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(),  0);
            DataStoreException exception = await Assert.ThrowsAsync<DataStoreException>(() => _dicomBlobDataStore.GetFileAsStreamAsync(id));
            Assert.Equal((int)HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenDeleted_DoesNotThrowException()
        {
            var id = new DicomInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 0);
            await _dicomBlobDataStore.DeleteFileIfExistsAsync(id);
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
