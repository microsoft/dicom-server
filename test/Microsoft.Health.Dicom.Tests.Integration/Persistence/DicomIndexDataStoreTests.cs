// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomIndexDataStoreTests : IClassFixture<DicomSqlIndexDataStoreTestsFixture>
    {
        private readonly IDicomIndexDataStore _dicomIndexDataStore;
        private readonly IDicomIndexDataStoreTestHelper _testHelper;
        private readonly DateTimeOffset _startDateTime = Clock.UtcNow;

        public DicomIndexDataStoreTests(DicomSqlIndexDataStoreTestsFixture fixture)
        {
            _dicomIndexDataStore = fixture.DicomIndexDataStore;
            _testHelper = fixture.TestHelper;
        }

        [Fact]
        public async Task GivenANonExistingDicomInstance_WhenAdded_ThenItShouldBeAdded()
        {
            DicomDataset dataset = CreateTestDicomDataset();
            string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);
            string patientId = dataset.GetString(DicomTag.PatientID);
            string patientName = dataset.GetString(DicomTag.PatientName);
            string referringPhysicianName = dataset.GetString(DicomTag.ReferringPhysicianName);
            string studyDescription = dataset.GetString(DicomTag.StudyDescription);
            string accessionNumber = dataset.GetString(DicomTag.AccessionNumber);
            string modality = dataset.GetString(DicomTag.Modality);

            long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dataset);

            IReadOnlyList<StudyMetadata> studyMetadataEntries = await _testHelper.GetStudyMetadataAsync(studyInstanceUid);

            Assert.Collection(
                studyMetadataEntries,
                entry => ValidateStudyMetadata(
                    studyInstanceUid,
                    0,
                    patientId,
                    patientName,
                    referringPhysicianName,
                    new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    studyDescription,
                    accessionNumber,
                    entry));

            IReadOnlyList<SeriesMetadata> seriesMetadataEntries = await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid);

            Assert.Collection(
                seriesMetadataEntries,
                entry => ValidateSeriesMetadata(
                    seriesInstanceUid,
                    0,
                    modality,
                    new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                    entry));

            // Make sure the ID matches between the study and series metadata.
            Assert.Equal(studyMetadataEntries[0].ID, seriesMetadataEntries[0].ID);

            IReadOnlyList<Instance> instances = await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            Assert.NotNull(instances);
            Assert.Single(instances);

            Instance instance = instances[0];

            Assert.Equal(studyInstanceUid, instance.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid, instance.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid, instance.SopInstanceUid);
            Assert.Equal(version, instance.Watermark);
            Assert.Equal((byte)DicomIndexStatus.Creating, instance.Status);
            Assert.InRange(instance.LastStatusUpdatedDate, _startDateTime.AddSeconds(-1), _startDateTime.AddSeconds(1));
            Assert.InRange(instance.CreatedDate, _startDateTime.AddSeconds(-1), _startDateTime.AddSeconds(1));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedByInstanceId_ThenItShouldBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid), ValidateSingleDeletedInstance(instance));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedByInstanceId_AdditionalInstancesShouldBeMaintained()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.NotEmpty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2));
            Assert.NotEmpty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.NotEmpty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, null), ValidateSingleDeletedInstance(instance));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedByInstanceId_AdditionalSeriesShouldBeMaintained()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            string seriesInstanceUid2 = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);

            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.NotEmpty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.NotEmpty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid2));
            Assert.NotEmpty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, null), ValidateSingleDeletedInstance(instance1));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedBySeriesId_ThenItShouldBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid), ValidateSingleDeletedInstance(instance));
        }

        [Fact]
        public async Task GivenMultipleDicomInstance_WhenDeletedBySeriesId_ThenItemsBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            Instance instance2 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, null), ValidateSingleDeletedInstance(instance2), ValidateSingleDeletedInstance(instance1));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedBySeriesId_AdditionalSeriesShouldBeMaintained()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            string seriesInstanceUid2 = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2);

            await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.NotEmpty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.NotEmpty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid2));
            Assert.NotEmpty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, null), ValidateSingleDeletedInstance(instance));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedByStudyId_ThenItShouldBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(seriesInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid), ValidateSingleDeletedInstance(instance));
        }

        [Fact]
        public async Task GivenMultipleDicomInstance_WhenDeletedByStudyUid_ThenItemsBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            Instance instance2 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            await _dicomIndexDataStore.DeleteStudyIndexAsync(studyInstanceUid, Clock.UtcNow, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(seriesInstanceUid));

            Assert.Collection(
                await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, null, null),
                ValidateSingleDeletedInstance(instance2),
                ValidateSingleDeletedInstance(instance1));
        }

        [Fact]
        public async Task GivenANonExistentInstance_WhenDeletedBySopInstanceUid_ThenExceptionThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(async () => await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate(), Clock.UtcNow, Clock.UtcNow));
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, Clock.UtcNow);
        }

        [Fact]
        public async Task GivenANonExistentSeries_WhenDeletedBySeriesInstanceUid_ThenExceptionThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(async () => await _dicomIndexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, TestUidGenerator.Generate(), Clock.UtcNow, Clock.UtcNow));
            await _dicomIndexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow, Clock.UtcNow);
        }

        [Fact]
        public async Task GivenANonExistentStudy_WhenDeletedByStudyInstanceUid_ThenExceptionThrown()
        {
            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(async () => await _dicomIndexDataStore.DeleteStudyIndexAsync(TestUidGenerator.Generate(), Clock.UtcNow, Clock.UtcNow));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenAdded_ThenConflictExceptionShouldBeThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            await _dicomIndexDataStore.CreateInstanceIndexAsync(dataset);

            await Assert.ThrowsAsync<DicomInstanceAlreadyExistsException>(() => _dicomIndexDataStore.CreateInstanceIndexAsync(dataset));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenStatusIsUpdated_ThenStatusShouldBeUpdated()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dataset);

            Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);

            Assert.NotNull(instance);

            DateTimeOffset lastStatusUpdatedDate = instance.LastStatusUpdatedDate;

            await _dicomIndexDataStore.UpdateInstanceIndexStatusAsync(
                new VersionedDicomInstanceIdentifier(
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    version),
                DicomIndexStatus.Created);

            IReadOnlyList<Instance> instances = await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            Assert.NotNull(instances);
            Assert.Single(instances);

            Instance updatedInstance = instances[0];

            Assert.Equal((byte)DicomIndexStatus.Created, updatedInstance.Status);
            Assert.True(updatedInstance.LastStatusUpdatedDate > lastStatusUpdatedDate);
        }

        [Fact]
        public async Task GivenANonExistingDicomInstance_WhenStatusIsUpdated_ThenItShouldThrowDicomInstanceNotFoundException()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dataset);

            VersionedDicomInstanceIdentifier dicomInstanceIdentifier = new VersionedDicomInstanceIdentifier(
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    version);

            await _dicomIndexDataStore.DeleteInstanceIndexAsync(dicomInstanceIdentifier);

            await Assert.ThrowsAsync<DicomInstanceNotFoundException>(
                () => _dicomIndexDataStore.UpdateInstanceIndexStatusAsync(dicomInstanceIdentifier, DicomIndexStatus.Created));

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
        }

        private static void ValidateStudyMetadata(
            string expectedStudyInstanceUid,
            int expectedVersion,
            string expectedPatientId,
            string expectedPatientName,
            string expectedReferringPhysicianName,
            DateTime? expectedStudyDate,
            string expectedStudyDescription,
            string expectedAccessionNumber,
            StudyMetadata actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedStudyInstanceUid, actual.StudyInstanceUid);
            Assert.Equal(expectedVersion, actual.Version);
            Assert.Equal(expectedPatientId, actual.PatientID);
            Assert.Equal(expectedPatientName, actual.PatientName);
            Assert.Equal(expectedReferringPhysicianName, actual.ReferringPhysicianName);
            Assert.Equal(expectedStudyDate, actual.StudyDate);
            Assert.Equal(expectedStudyDescription, actual.StudyDescription);
            Assert.Equal(expectedAccessionNumber, actual.AccessionNumber);
        }

        private static void ValidateSeriesMetadata(
            string expectedSeriesInstanceUid,
            int expectedVersion,
            string expectedModality,
            DateTime? expectedPerformedProcedureStepStartDate,
            SeriesMetadata actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedSeriesInstanceUid, actual.SeriesInstanceUid);
            Assert.Equal(expectedVersion, actual.Version);
            Assert.Equal(expectedModality, actual.Modality);
            Assert.Equal(expectedPerformedProcedureStepStartDate, actual.PerformedProcedureStepStartDate);
        }

        private Action<DeletedInstance> ValidateSingleDeletedInstance(Instance instance)
        {
            return deletedInstance =>
            {
                Assert.Equal(instance.StudyInstanceUid, deletedInstance.StudyInstanceUid);
                Assert.Equal(instance.SeriesInstanceUid, deletedInstance.SeriesInstanceUid);
                Assert.Equal(instance.SopInstanceUid, deletedInstance.SopInstanceUid);
                Assert.Equal(instance.Watermark, deletedInstance.Watermark);
                Assert.InRange(deletedInstance.DeletedDateTime, _startDateTime.AddSeconds(-1), _startDateTime.AddSeconds(1));
                Assert.Equal(0, deletedInstance.RetryCount);
                Assert.InRange(deletedInstance.CleanupAfter, _startDateTime.AddSeconds(-1), _startDateTime.AddSeconds(1));
            };
        }

        private static DicomDataset CreateTestDicomDataset(string studyInstanceUid = null, string seriesInstanceUid = null, string sopInstanceUid = null)
        {
            if (string.IsNullOrEmpty(studyInstanceUid))
            {
                studyInstanceUid = TestUidGenerator.Generate();
            }

            if (string.IsNullOrEmpty(seriesInstanceUid))
            {
                seriesInstanceUid = TestUidGenerator.Generate();
            }

            if (string.IsNullOrEmpty(sopInstanceUid))
            {
                sopInstanceUid = TestUidGenerator.Generate();
            }

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            dataset.Remove(DicomTag.PatientID);

            dataset.Add(DicomTag.PatientID, "pid");
            dataset.Add(DicomTag.PatientName, "pname");
            dataset.Add(DicomTag.ReferringPhysicianName, "rname");
            dataset.Add(DicomTag.StudyDate, "20200301");
            dataset.Add(DicomTag.StudyDescription, "sd");
            dataset.Add(DicomTag.AccessionNumber, "an");
            dataset.Add(DicomTag.Modality, "M");
            dataset.Add(DicomTag.PerformedProcedureStepStartDate, "20200302");
            return dataset;
        }

        private async Task<Instance> CreateIndexAndVerifyInstance(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            DicomDataset dataset = CreateTestDicomDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            long version = await _dicomIndexDataStore.CreateInstanceIndexAsync(dataset);
            Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);
            Assert.Equal(sopInstanceUid, instance.SopInstanceUid);
            return instance;
        }
    }
}
