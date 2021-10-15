// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DeleteServiceTests : IClassFixture<DeleteServiceTestsFixture>
    {
        private readonly DeleteServiceTestsFixture _fixture;

        public DeleteServiceTests(DeleteServiceTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task GivenDeletedInstance_WhenCleanupCalledWithDifferentStorePersistence_FilesAndTriesAreRemoved(bool persistBlob, bool persistMetadata)
        {
            var dicomInstanceIdentifier = await CreateAndValidateValuesInStores(persistBlob, persistMetadata);
            await DeleteAndValidateInstanceForCleanup(dicomInstanceIdentifier);

            await Task.Delay(3000, CancellationToken.None);
            (bool success, int retrievedInstanceCount) = await _fixture.DeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            await ValidateRemoval(success, retrievedInstanceCount, dicomInstanceIdentifier);
        }

        private async Task DeleteAndValidateInstanceForCleanup(VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            await _fixture.DeleteService.DeleteInstanceAsync(versionedInstanceIdentifier.StudyInstanceUid, versionedInstanceIdentifier.SeriesInstanceUid, versionedInstanceIdentifier.SopInstanceUid, CancellationToken.None);

            Assert.NotEmpty(await _fixture.IndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(versionedInstanceIdentifier.StudyInstanceUid, versionedInstanceIdentifier.SeriesInstanceUid, versionedInstanceIdentifier.SopInstanceUid));
        }

        private async Task<VersionedInstanceIdentifier> CreateAndValidateValuesInStores(bool persistBlob, bool persistMetadata)
        {
            var newDataSet = CreateValidMetadataDataset();

            var version = await _fixture.IndexDataStore.BeginCreateInstanceIndexAsync(null, newDataSet);
            var versionedDicomInstanceIdentifier = newDataSet.ToVersionedInstanceIdentifier(version);

            if (persistMetadata)
            {
                await _fixture.MetadataStore.StoreInstanceMetadataAsync(newDataSet, versionedDicomInstanceIdentifier.Version);

                var metaEntry = await _fixture.MetadataStore.GetInstanceMetadataAsync(versionedDicomInstanceIdentifier);
                Assert.Equal(versionedDicomInstanceIdentifier.SopInstanceUid, metaEntry.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            }

            if (persistBlob)
            {
                var fileData = new byte[] { 4, 7, 2 };

                await using (MemoryStream stream = _fixture.RecyclableMemoryStreamManager.GetStream("GivenDeletedInstances_WhenCleanupCalled_FilesAndTriesAreRemoved.fileData", fileData, 0, fileData.Length))
                {
                    Uri fileLocation = await _fixture.FileStore.StoreFileAsync(versionedDicomInstanceIdentifier, stream);

                    Assert.NotNull(fileLocation);
                }

                var file = await _fixture.FileStore.GetFileAsync(versionedDicomInstanceIdentifier);

                Assert.NotNull(file);
            }

            return versionedDicomInstanceIdentifier;
        }

        private async Task ValidateRemoval(bool success, int retrievedInstanceCount, VersionedInstanceIdentifier versionedInstanceIdentifier)
        {
            Assert.True(success);
            Assert.Equal(1, retrievedInstanceCount);

            await Assert.ThrowsAsync<ItemNotFoundException>(async () => await _fixture.MetadataStore.GetInstanceMetadataAsync(versionedInstanceIdentifier));
            await Assert.ThrowsAsync<ItemNotFoundException>(async () => await _fixture.FileStore.GetFileAsync(versionedInstanceIdentifier));

            Assert.Empty(await _fixture.IndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(versionedInstanceIdentifier.StudyInstanceUid, versionedInstanceIdentifier.SeriesInstanceUid, versionedInstanceIdentifier.SopInstanceUid));
        }

        private static DicomDataset CreateValidMetadataDataset()
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SeriesInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
            };
        }
    }
}
