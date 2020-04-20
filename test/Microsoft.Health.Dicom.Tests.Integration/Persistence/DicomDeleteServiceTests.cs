// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Exceptions;
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
            var dicomInstanceIdentifier = CreateInstanceIdentifier();
            await CreateAndValidateValuesInStores(dicomInstanceIdentifier, persistBlob, persistMetadata);
            await DeleteAndValidateInstanceForCleanup(dicomInstanceIdentifier);

            await Task.Delay(3000, CancellationToken.None);
            (bool success, int rowsProcessed) = await _fixture.DicomDeleteService.CleanupDeletedInstancesAsync();

            await ValidateRemoval(success, rowsProcessed, dicomInstanceIdentifier);
        }

        private async Task DeleteAndValidateInstanceForCleanup(VersionedDicomInstanceIdentifier dicomInstanceIdentifier)
        {
            await _fixture.DicomDeleteService.DeleteInstanceAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, CancellationToken.None);

            Assert.NotEmpty(await _fixture.DicomIndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid));
        }

        private async Task CreateAndValidateValuesInStores(VersionedDicomInstanceIdentifier dicomInstanceIdentifier, bool persistBlob, bool persistMetadata)
        {
            var newDataSet = CreateValidMetadataDataset(dicomInstanceIdentifier);

            await _fixture.DicomIndexDataStore.IndexInstanceAsync(newDataSet);

            if (persistMetadata)
            {
                await _fixture.DicomMetadataStore.AddInstanceMetadataAsync(newDataSet);

                var metaEntry = await _fixture.DicomMetadataStore.GetInstanceMetadataAsync(dicomInstanceIdentifier);
                Assert.Equal(dicomInstanceIdentifier.SopInstanceUid, metaEntry.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            }

            if (persistBlob)
            {
                var fileName = DicomBlobFileStore.GetBlobStorageName(dicomInstanceIdentifier);
                var fileData = new byte[] { 4, 7, 2 };

                await using (MemoryStream stream = _fixture.RecyclableMemoryStreamManager.GetStream("GivenDeletedInstances_WhenCleanupCalled_FilesAndTriesAreRemoved.fileData", fileData, 0, fileData.Length))
                {
                    Uri fileLocation = await _fixture.DicomFileStore.AddAsync(dicomInstanceIdentifier, stream);

                    Assert.NotNull(fileLocation);
                    Assert.EndsWith(fileName, fileLocation.AbsoluteUri);
                }

                var file = await _fixture.DicomFileStore.GetAsync(dicomInstanceIdentifier);

                Assert.NotNull(file);
            }
        }

        private async Task ValidateRemoval(bool success, int rowsProcessed, VersionedDicomInstanceIdentifier dicomInstanceIdentifier)
        {
            Assert.True(success);
            Assert.Equal(1, rowsProcessed);

            await Assert.ThrowsAsync<DicomDataStoreException>(async () => await _fixture.DicomMetadataStore.GetInstanceMetadataAsync(dicomInstanceIdentifier));
            await Assert.ThrowsAsync<DicomDataStoreException>(async () => await _fixture.DicomFileStore.GetAsync(dicomInstanceIdentifier));

            Assert.Empty(await _fixture.DicomIndexDataStoreTestHelper.GetDeletedInstanceEntriesAsync(dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid));
        }

        private VersionedDicomInstanceIdentifier CreateInstanceIdentifier()
        {
            return new VersionedDicomInstanceIdentifier(
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate(),
                1);
        }

        private DicomDataset CreateValidMetadataDataset(VersionedDicomInstanceIdentifier versionedDicomInstanceIdentifier)
        {
            return new DicomDataset()
            {
                { DicomTag.StudyInstanceUID, versionedDicomInstanceIdentifier.StudyInstanceUid },
                { DicomTag.SeriesInstanceUID, versionedDicomInstanceIdentifier.SeriesInstanceUid },
                { DicomTag.SOPInstanceUID, versionedDicomInstanceIdentifier.SopInstanceUid },
                { DicomTag.PatientID, TestUidGenerator.Generate() },
            };
        }
    }
}
