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
        public async Task GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted()
        {
            VersionedInstanceIdentifier id = GenerateIdentifier();

            var fileData = new byte[] { 4, 7, 2 };

            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream("GivenAValidFileStream_WhenStored_CanBeRetrievedAndDeleted.fileData", fileData, 0, fileData.Length))
            {
                Uri fileLocation = await AddFileAsync(id, stream);

                Assert.NotNull(fileLocation);
            }

            await using (Stream resultStream = await _blobDataStore.GetFileAsync(id))
            {
                byte[] result = await ConvertStreamToByteArrayAsync(resultStream);
                Assert.Equal(fileData, result);
            }

            await _blobDataStore.DeleteFileIfExistsAsync(id);
        }

        [Fact]
        public async Task GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists()
        {
            VersionedInstanceIdentifier id = GenerateIdentifier();

            var fileData = new byte[] { 4, 7, 2 };

            await using (var stream = _recyclableMemoryStreamManager.GetStream("GivenAValidFile_WhenStored_CanBeOverwrittenOrThrowExceptionIsExists.fileData", fileData, 0, fileData.Length))
            {
                // Overwrite test
                Uri fileLocation1 = await AddFileAsync(id, stream);
                Uri fileLocation2 = await AddFileAsync(id, stream, overwriteIfExists: true);
                Assert.Equal(fileLocation1, fileLocation2);

                // Fail on exists
                await Assert.ThrowsAsync<InstanceAlreadyExistsException>(
                    () => AddFileAsync(id, stream, overwriteIfExists: false));
            }

            await _blobDataStore.DeleteFileIfExistsAsync(id);
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenFetched_ThrowsNotFoundException()
        {
            VersionedInstanceIdentifier id = GenerateIdentifier();

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _blobDataStore.GetFileAsync(id));
        }

        [Fact]
        public async Task GivenANonExistentFile_WhenDeleted_DoesNotThrowException()
        {
            VersionedInstanceIdentifier id = GenerateIdentifier();

            await _blobDataStore.DeleteFileIfExistsAsync(id);
        }

        private async Task<byte[]> ConvertStreamToByteArrayAsync(Stream stream)
        {
            await using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private VersionedInstanceIdentifier GenerateIdentifier()
            => new VersionedInstanceIdentifier(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                version: 0);

        private Task<Uri> AddFileAsync(VersionedInstanceIdentifier instanceIdentifier, Stream stream, bool overwriteIfExists = false, CancellationToken cancellationToken = default)
            => _blobDataStore.AddFileAsync(instanceIdentifier, stream, overwriteIfExists, cancellationToken);
    }
}
