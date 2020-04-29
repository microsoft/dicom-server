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
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomDeleteServiceTests : IClassFixture<DicomDeleteServiceTestsFixture>
    {
        private readonly DicomDeleteServiceTestsFixture _fixture;

        public DicomDeleteServiceTests(DicomDeleteServiceTestsFixture fixture)
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
            (bool success, int retrievedInstanceCount) = await _fixture.DicomDeleteService.CleanupDeletedInstancesAsync(CancellationToken.None);

            await ValidateRemoval(success, retrievedInstanceCount, dicomInstanceIdentifier);
        }

        private async Task DeleteAndValidateInstanceForCleanup(VersionedDicomInstanceIdentifier dicomInstanceIdentifier)
        {
            await _fixture.DicomDeleteService.DeleteInstanceAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, CancellationToken.None);

            Assert.NotEmpty(await _fixture.DicomIndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid));
        }

        private async Task<VersionedDicomInstanceIdentifier> CreateAndValidateValuesInStores(bool persistBlob, bool persistMetadata)
        {
            var newDataSet = CreateValidMetadataDataset();

            var version = await _fixture.DicomIndexDataStore.CreateInstanceIndexAsync(newDataSet);
            var versionedDicomInstanceIdentifier = newDataSet.ToVersionedDicomInstanceIdentifier(version);

            if (persistMetadata)
            {
                await _fixture.DicomMetadataStore.AddInstanceMetadataAsync(newDataSet, versionedDicomInstanceIdentifier.Version);

                var metaEntry = await _fixture.DicomMetadataStore.GetInstanceMetadataAsync(versionedDicomInstanceIdentifier);
                Assert.Equal(versionedDicomInstanceIdentifier.SopInstanceUid, metaEntry.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            }

            if (persistBlob)
            {
                var fileData = new byte[] { 4, 7, 2 };

                await using (MemoryStream stream = _fixture.RecyclableMemoryStreamManager.GetStream("GivenDeletedInstances_WhenCleanupCalled_FilesAndTriesAreRemoved.fileData", fileData, 0, fileData.Length))
                {
                    Uri fileLocation = await _fixture.DicomFileStore.AddFileAsync(versionedDicomInstanceIdentifier, stream);

                    Assert.NotNull(fileLocation);
                }

                var file = await _fixture.DicomFileStore.GetFileAsync(versionedDicomInstanceIdentifier);

                Assert.NotNull(file);
            }

            return versionedDicomInstanceIdentifier;
        }

        private async Task ValidateRemoval(bool success, int retrievedInstanceCount, VersionedDicomInstanceIdentifier dicomInstanceIdentifier)
        {
            Assert.True(success);
            Assert.Equal(1, retrievedInstanceCount);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(async () => await _fixture.DicomMetadataStore.GetInstanceMetadataAsync(dicomInstanceIdentifier));
            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(async () => await _fixture.DicomFileStore.GetFileAsync(dicomInstanceIdentifier));

            Assert.Empty(await _fixture.DicomIndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid));
        }

        private DicomDataset CreateValidMetadataDataset()
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
