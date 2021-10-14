// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    ///  Tests for IndexDataStore.
    /// </summary>
    public partial class IndexDataStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
    {
        private readonly IIndexDataStore _indexDataStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexDataStoreTestHelper _testHelper;
        private readonly IExtendedQueryTagStoreTestHelper _extendedQueryTagStoreTestHelper;
        private readonly DateTimeOffset _startDateTime = Clock.UtcNow;

        public IndexDataStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            EnsureArg.IsNotNull(fixture.IndexDataStoreTestHelper, nameof(fixture.IndexDataStoreTestHelper));
            EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreTestHelper, nameof(fixture.ExtendedQueryTagStoreTestHelper));
            _indexDataStore = fixture.IndexDataStore;
            _extendedQueryTagStore = fixture.ExtendedQueryTagStore;
            _testHelper = fixture.IndexDataStoreTestHelper;
            _extendedQueryTagStoreTestHelper = fixture.ExtendedQueryTagStoreTestHelper;
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

            long version = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);

            IReadOnlyList<StudyMetadata> studyMetadataEntries = await _testHelper.GetStudyMetadataAsync(studyInstanceUid);

            Assert.Collection(
                studyMetadataEntries,
                entry => ValidateStudyMetadata(
                    studyInstanceUid,
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
                    modality,
                    new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                    entry));

            // Make sure the ID matches between the study and series metadata.
            Assert.Equal(studyMetadataEntries[0].StudyKey, seriesMetadataEntries[0].StudyKey);

            IReadOnlyList<Instance> instances = await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            Assert.NotNull(instances);
            Assert.Single(instances);

            Instance instance = instances[0];

            Assert.Equal(studyInstanceUid, instance.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid, instance.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid, instance.SopInstanceUid);
            Assert.Equal(version, instance.Watermark);
            Assert.Equal((byte)IndexStatus.Creating, instance.Status);
            Assert.InRange(instance.LastStatusUpdatedDate, _startDateTime.AddSeconds(-1), Clock.UtcNow.AddSeconds(1));
            Assert.InRange(instance.CreatedDate, _startDateTime.AddSeconds(-1), Clock.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task GivenANewDicomInstance_WhenConflictingStudyAndSeriesTags_ThenLatestWins()
        {
            // create a new instance
            DicomDataset dataset = CreateTestDicomDataset();
            string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);

            // add another instance in the same study+series with different patientName and modality and validate latest wins
            string conflictPatientName = "pname^conflict";
            string conflictModality = "MCONFLICT";
            string newInstance = TestUidGenerator.Generate();
            dataset.AddOrUpdate(DicomTag.PatientName, conflictPatientName);
            dataset.AddOrUpdate(DicomTag.Modality, conflictModality);
            dataset.AddOrUpdate(DicomTag.SOPInstanceUID, newInstance);

            await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);

            IReadOnlyList<StudyMetadata> studyMetadataEntries = await _testHelper.GetStudyMetadataAsync(studyInstanceUid);
            IReadOnlyList<SeriesMetadata> seriesMetadataEntries = await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid);

            Assert.Equal(1, studyMetadataEntries.Count);
            Assert.Equal(conflictPatientName, studyMetadataEntries.First().PatientName);

            Assert.Equal(1, seriesMetadataEntries.Count);
            Assert.Equal(conflictModality, seriesMetadataEntries.First().Modality);
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenDeletedByInstanceId_ThenItShouldBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

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

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

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

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

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

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow);

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

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

            Assert.Collection(
                await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, null),
                ValidateSingleDeletedInstance(instance1),
                ValidateSingleDeletedInstance(instance2));
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

            await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow);

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

            await _indexDataStore.DeleteStudyIndexAsync(studyInstanceUid, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(seriesInstanceUid));

            Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid), ValidateSingleDeletedInstance(instance));
        }

        [Fact]
        public async Task GivenMultipleDicomInstance_WhenDeletedByStudyInstanceUid_ThenItemsBeRemovedAndAddedToDeletedInstanceTable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            Instance instance2 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            await _indexDataStore.DeleteStudyIndexAsync(studyInstanceUid, Clock.UtcNow);

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2));
            Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
            Assert.Empty(await _testHelper.GetStudyMetadataAsync(seriesInstanceUid));

            Assert.Collection(
                await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, null, null),
                ValidateSingleDeletedInstance(instance1),
                ValidateSingleDeletedInstance(instance2));
        }

        [Fact]
        public async Task GivenANonExistentInstance_WhenDeletedBySopInstanceUid_ThenExceptionThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate(), Clock.UtcNow));
            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);
        }

        [Fact]
        public async Task GivenANonExistentSeries_WhenDeletedBySeriesInstanceUid_ThenExceptionThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await Assert.ThrowsAsync<SeriesNotFoundException>(() => _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, TestUidGenerator.Generate(), Clock.UtcNow));
            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);
        }

        [Fact]
        public async Task GivenANonExistentStudy_WhenDeletedByStudyInstanceUid_ThenExceptionThrown()
        {
            await Assert.ThrowsAsync<StudyNotFoundException>(() => _indexDataStore.DeleteStudyIndexAsync(TestUidGenerator.Generate(), Clock.UtcNow));
        }

        [Fact]
        public async Task GivenAPendingDicomInstance_WhenAdded_ThenPendingDicomInstanceExceptionShouldBeThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);

            await Assert.ThrowsAsync<PendingInstanceException>(() => _indexDataStore.BeginCreateInstanceIndexAsync(dataset));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenAdded_ThenDicomInstanceAlreadyExistsExceptionShouldBeThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            long version = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);
            await _indexDataStore.EndCreateInstanceIndexAsync(dataset, version);

            await Assert.ThrowsAsync<InstanceAlreadyExistsException>(() => _indexDataStore.BeginCreateInstanceIndexAsync(dataset));
        }

        [Fact]
        public async Task GivenAnExistingDicomInstance_WhenStatusIsUpdated_ThenStatusShouldBeUpdated()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            long version = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);

            Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);

            Assert.NotNull(instance);

            DateTimeOffset lastStatusUpdatedDate = instance.LastStatusUpdatedDate;

            // Make sure there is delay between.
            await Task.Delay(50);

            await _indexDataStore.EndCreateInstanceIndexAsync(dataset, version);

            IReadOnlyList<Instance> instances = await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            Assert.NotNull(instances);
            Assert.Single(instances);

            Instance updatedInstance = instances[0];

            Assert.Equal((byte)IndexStatus.Created, updatedInstance.Status);
            Assert.True(updatedInstance.LastStatusUpdatedDate > lastStatusUpdatedDate);
        }

        [Fact]
        public async Task GivenANonExistingDicomInstance_WhenStatusIsUpdated_ThenDicomInstanceNotFoundExceptionShouldBeThrown()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

            long version = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);

            VersionedInstanceIdentifier versionedInstanceIdentifier = new VersionedInstanceIdentifier(
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    version);

            await _indexDataStore.DeleteInstanceIndexAsync(versionedInstanceIdentifier);

            await Assert.ThrowsAsync<InstanceNotFoundException>(
                () => _indexDataStore.EndCreateInstanceIndexAsync(dataset, version));

            Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
        }

        [Fact]
        public async Task GivenADeletedDicomInstance_WhenIncrementingRetryCount_NewRetryCountShouldBeReturned()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

            DeletedInstance deletedEntry = (await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid)).First();
            var versionedDicomInstanceIdentifier = new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, deletedEntry.Watermark);
            var retryCount = await _indexDataStore.IncrementDeletedInstanceRetryAsync(versionedDicomInstanceIdentifier, Clock.UtcNow);
            Assert.Equal(1, retryCount);
        }

        [Fact]
        public async Task GivenNoDeletedInstances_NumMatchRetryCountShouldBe0()
        {
            await _testHelper.ClearDeletedInstanceTableAsync();
            var numMatchRetryCount = await _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(0);
            Assert.Equal(0, numMatchRetryCount);
        }

        [Fact]
        public async Task GivenFewDeletedInstances_NumMatchRetryCountShouldBeCorrect()
        {
            await _testHelper.ClearDeletedInstanceTableAsync();

            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            Instance instance2 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2, Clock.UtcNow);

            var numMatchRetryCount = await _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(0);
            Assert.Equal(2, numMatchRetryCount);
        }

        [Fact]
        public async Task GivenNoDeletedInstances_OldestDeletedIsCurrentTime()
        {
            await _testHelper.ClearDeletedInstanceTableAsync();

            Assert.InRange(await _indexDataStore.GetOldestDeletedAsync(), Clock.UtcNow.AddSeconds(-1), Clock.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task GivenMultipleDeletedInstances_OldestDeletedIsCorrect()
        {
            await _testHelper.ClearDeletedInstanceTableAsync();

            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            // Delete first entry
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);
            DeletedInstance first = (await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid)).Single();

            // Create and delete another entry
            await Task.Delay(1000);

            string sopInstanceUid2 = TestUidGenerator.Generate();
            await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);
            await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2, Clock.UtcNow);
            DeletedInstance second = (await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid2)).Single();

            Assert.True(first.DeletedDateTime < second.DeletedDateTime);
            Assert.True(await _indexDataStore.GetOldestDeletedAsync() <= first.DeletedDateTime);
        }

        [Fact]
        public async Task GivenNoExtendedQueryTags_WhenCreateIndex_ThenShouldSucceed()
        {
            var extendedTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(int.MaxValue);
            // make sure there is no extended query tags
            Assert.Empty(extendedTags);

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            await _indexDataStore.BeginCreateInstanceIndexAsync(dataset, QueryTagService.CoreQueryTags);
        }

        [Fact]
        public async Task GivenMaxTagKeyNotMatch_WhenCreateIndex_ThenShouldThrowException()
        {
            AddExtendedQueryTagEntry extendedQueryTagEntry = DicomTag.PatientAge.BuildAddExtendedQueryTagEntry();
            var tagEntry = (await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new[] { extendedQueryTagEntry }, maxAllowedCount: 128, ready: true))[0];
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            // Add a new tag
            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new[] { DicomTag.PatientName.BuildAddExtendedQueryTagEntry() }, maxAllowedCount: 128, ready: true);

            var queryTags = new[] { new QueryTag(tagEntry) };
            long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset, queryTags);
            await Assert.ThrowsAsync<ExtendedQueryTagsOutOfDateException>(
                () => _indexDataStore.EndCreateInstanceIndexAsync(dataset, watermark, queryTags));
        }

        private static void ValidateStudyMetadata(
            string expectedStudyInstanceUid,
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
            Assert.Equal(expectedPatientId, actual.PatientID);
            Assert.Equal(expectedPatientName, actual.PatientName);
            Assert.Equal(expectedReferringPhysicianName, actual.ReferringPhysicianName);
            Assert.Equal(expectedStudyDate, actual.StudyDate);
            Assert.Equal(expectedStudyDescription, actual.StudyDescription);
            Assert.Equal(expectedAccessionNumber, actual.AccessionNumber);
        }

        private static void ValidateSeriesMetadata(
            string expectedSeriesInstanceUid,
            string expectedModality,
            DateTime? expectedPerformedProcedureStepStartDate,
            SeriesMetadata actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedSeriesInstanceUid, actual.SeriesInstanceUid);
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
                Assert.InRange(deletedInstance.DeletedDateTime, _startDateTime.AddSeconds(-1), Clock.UtcNow.AddSeconds(1));
                Assert.Equal(0, deletedInstance.RetryCount);
                Assert.InRange(deletedInstance.CleanupAfter, _startDateTime.AddSeconds(-1), Clock.UtcNow.AddSeconds(1));
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
            long version = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);
            Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);
            Assert.Equal(sopInstanceUid, instance.SopInstanceUid);
            return instance;
        }


        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _testHelper.ClearIndexTablesAsync();
            await _extendedQueryTagStoreTestHelper.ClearExtendedQueryTagTablesAsync();
        }
    }
}
