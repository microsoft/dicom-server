// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

/// <summary>
///  Tests for IndexDataStore.
/// </summary>
public partial class IndexDataStoreTests : IClassFixture<SqlDataStoreTestsFixture>, IAsyncLifetime
{
    private readonly IIndexDataStore _indexDataStore;
    private readonly IPartitionStore _partitionStore;
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly IIndexDataStoreTestHelper _testHelper;
    private readonly IExtendedQueryTagStoreTestHelper _extendedQueryTagStoreTestHelper;
    private readonly DateTimeOffset _startDateTime = DateTimeOffset.UtcNow;
    private readonly FileProperties _defaultFileProperties = new() { Path = "partitionA/123.dcm", ETag = "e456", ContentLength = 123 };

    public IndexDataStoreTests(SqlDataStoreTestsFixture fixture)
    {
        EnsureArg.IsNotNull(fixture, nameof(fixture));
        EnsureArg.IsNotNull(fixture.IndexDataStore, nameof(fixture.IndexDataStore));
        EnsureArg.IsNotNull(fixture.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
        EnsureArg.IsNotNull(fixture.IndexDataStoreTestHelper, nameof(fixture.IndexDataStoreTestHelper));
        EnsureArg.IsNotNull(fixture.ExtendedQueryTagStoreTestHelper, nameof(fixture.ExtendedQueryTagStoreTestHelper));
        _indexDataStore = fixture.IndexDataStore;
        _partitionStore = fixture.PartitionStore;
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

        long version = await _indexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), dataset);

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
        Assert.InRange(instance.LastStatusUpdatedDate, _startDateTime.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.InRange(instance.CreatedDate, _startDateTime.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task WhenAddedWithInstancesInDifferentPartitions_ThenTheyShouldBeAdded()
    {
        DicomDataset dataset = CreateTestDicomDataset();
        string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
        string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
        string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);
        string partitionName1 = TestUidGenerator.Generate();
        string partitionName2 = TestUidGenerator.Generate();

        var partition1 = await _partitionStore.AddPartitionAsync(partitionName1);
        var partition2 = await _partitionStore.AddPartitionAsync(partitionName2);

        await _indexDataStore.BeginCreateInstanceIndexAsync(partition1, dataset);
        await _indexDataStore.BeginCreateInstanceIndexAsync(partition2, dataset);

        IReadOnlyList<Instance> instances1 = await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, partition1.Key);
        IReadOnlyList<Instance> instances2 = await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, partition2.Key);

        Assert.NotNull(instances1);
        Assert.Single(instances1);
        Assert.NotNull(instances2);
        Assert.Single(instances2);

        Instance instance1 = instances1[0];
        Instance instance2 = instances2[0];

        Assert.Equal(partition1.Key, instance1.PartitionKey);
        Assert.Equal(partition2.Key, instance2.PartitionKey);
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

        await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset);

        IReadOnlyList<StudyMetadata> studyMetadataEntries = await _testHelper.GetStudyMetadataAsync(studyInstanceUid);
        IReadOnlyList<SeriesMetadata> seriesMetadataEntries = await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid);

        Assert.Single(studyMetadataEntries);
        Assert.Equal(conflictPatientName, studyMetadataEntries.First().PatientName);

        Assert.Single(seriesMetadataEntries);
        Assert.Equal(conflictModality, seriesMetadataEntries.First().Modality);
    }

    [Fact]
    public async Task GivenANewDicomInstance_WhenConflictingStudyAndSeriesTagsWithNulls_PreviousDataStays()
    {
        // create a new instance
        DicomDataset dataset = CreateTestDicomDataset();
        string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
        string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
        long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset);
        await _indexDataStore.EndCreateInstanceIndexAsync(1, dataset, watermark, Array.Empty<QueryTag>());

        // add another instance in the same study+series with null patientName and modality and validate previous data wins
        DicomDataset newdataset = CreateTestDicomDataset(studyInstanceUid, seriesInstanceUid);
        string conflictPatientName = null;
        string conflictStudyDescription = null;
        string conflictModality = null;
        newdataset.AddOrUpdate(DicomTag.PatientName, conflictPatientName);
        newdataset.AddOrUpdate(DicomTag.Modality, conflictModality);
        newdataset.AddOrUpdate(DicomTag.StudyDescription, conflictStudyDescription);
        await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, newdataset);

        IReadOnlyList<StudyMetadata> studyMetadataEntries = await _testHelper.GetStudyMetadataAsync(studyInstanceUid);
        IReadOnlyList<SeriesMetadata> seriesMetadataEntries = await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid);

        Assert.Single(studyMetadataEntries);
        Assert.Equal(dataset.GetString(DicomTag.PatientName), studyMetadataEntries.First().PatientName);
        Assert.Equal(dataset.GetString(DicomTag.StudyDescription), studyMetadataEntries.First().StudyDescription);

        Assert.Single(seriesMetadataEntries);
        Assert.Equal(dataset.GetString(DicomTag.Modality), seriesMetadataEntries.First().Modality);
    }

    [Fact]
    public async Task GivenAnExistingDicomInstance_WhenDeletedByInstanceId_ThenItShouldBeRemovedAndAddedToDeletedInstanceTable()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();
        Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);

        Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
        Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
        Assert.Empty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

        Assert.Collection(await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid), ValidateSingleDeletedInstance(instance));
    }

    [Fact]
    public async Task GivenAnExistingDicomInstanceWithExternalStoreUsed_WhenDeletedByInstanceId_ThenItShouldHaveFilePropertiesAddedToDeletedInstanceTable()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();
        Instance instance = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid, filePropertiesToCreate: _defaultFileProperties);

        Assert.NotEmpty(await _testHelper.GetFilePropertiesAsync(instance.Watermark));

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);

        Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
        Assert.Empty(await _testHelper.GetSeriesMetadataAsync(seriesInstanceUid));
        Assert.Empty(await _testHelper.GetStudyMetadataAsync(studyInstanceUid));

        Assert.Collection(
            await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid),
            ValidateSingleDeletedInstance(instance, expectedFileProperties: _defaultFileProperties));
    }

    [Fact]
    public async Task GivenADicomInstanceWithFileProperties_WhenStored_ThenFilePropertiesOnInstanceShouldBeStored()
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset();
        var identifier = dataset.ToInstanceIdentifier(Partition.Default);
        Instance instance = await CreateIndexAndVerifyInstance(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid);
        await _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, instance.Watermark, Array.Empty<QueryTag>(), _defaultFileProperties);

        FileProperty storedFileProperty = (await _testHelper.GetFilePropertiesAsync(instance.Watermark)).Single();

        Assert.Equal(storedFileProperty.FilePath, _defaultFileProperties.Path);
        Assert.Equal(storedFileProperty.ETag, _defaultFileProperties.ETag);
        Assert.Equal(storedFileProperty.ContentLength, _defaultFileProperties.ContentLength);
    }

    [Fact]
    public async Task GivenAnExistingDicomInstanceWithFileProperties_WhenDeletedByInstanceId_ThenFilePropertiesOnInstanceShouldBeDeleted()
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset();
        var identifier = dataset.ToInstanceIdentifier(Partition.Default);
        Instance instance = await CreateIndexAndVerifyInstance(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid);
        await _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, instance.Watermark, Array.Empty<QueryTag>(), _defaultFileProperties);

        Assert.NotEmpty(await _testHelper.GetFilePropertiesAsync(instance.Watermark));

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, DateTimeOffset.UtcNow);

        Assert.Empty(await _testHelper.GetFilePropertiesAsync(instance.Watermark));

        Assert.Collection(
            await _testHelper.GetDeletedInstanceEntriesAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid),
            ValidateSingleDeletedInstance(instance));
    }

    [Fact]
    public async Task GivenAnExistingDicomInstanceWithoutFileProperties_WhenDeletedByInstanceId_ThenInstanceStillDeleted()
    {
        DicomDataset dataset = Samples.CreateRandomInstanceDataset();
        var identifier = dataset.ToInstanceIdentifier(Partition.Default);
        Instance instance = await CreateIndexAndVerifyInstance(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid);
        await _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, instance.Watermark, Array.Empty<QueryTag>(), _defaultFileProperties);

        Assert.NotEmpty(await _testHelper.GetFilePropertiesAsync(instance.Watermark));

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, DateTimeOffset.UtcNow);

        Assert.Empty(await _testHelper.GetFilePropertiesAsync(instance.Watermark));

        Assert.Collection(
            await _testHelper.GetDeletedInstanceEntriesAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid),
            ValidateSingleDeletedInstance(instance));
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

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);

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

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);

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

        await _indexDataStore.DeleteSeriesIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, DateTimeOffset.UtcNow);

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

        await _indexDataStore.DeleteSeriesIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, DateTimeOffset.UtcNow);

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

        await _indexDataStore.DeleteSeriesIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, DateTimeOffset.UtcNow);

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

        await _indexDataStore.DeleteStudyIndexAsync(Partition.Default, studyInstanceUid, DateTimeOffset.UtcNow);

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

        await _indexDataStore.DeleteStudyIndexAsync(Partition.Default, studyInstanceUid, DateTimeOffset.UtcNow);

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

        await Assert.ThrowsAsync<InstanceNotFoundException>(() => _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, TestUidGenerator.Generate(), DateTimeOffset.UtcNow));
        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GivenANonExistentSeries_WhenDeletedBySeriesInstanceUid_ThenExceptionThrown()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();
        await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        await Assert.ThrowsAsync<SeriesNotFoundException>(() => _indexDataStore.DeleteSeriesIndexAsync(Partition.Default, studyInstanceUid, TestUidGenerator.Generate(), DateTimeOffset.UtcNow));
        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GivenANonExistentStudy_WhenDeletedByStudyInstanceUid_ThenExceptionThrown()
    {
        await Assert.ThrowsAsync<StudyNotFoundException>(() => _indexDataStore.DeleteStudyIndexAsync(Partition.Default, TestUidGenerator.Generate(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task GivenAPendingDicomInstance_WhenAdded_ThenPendingDicomInstanceExceptionShouldBeThrown()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

        await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset);

        await Assert.ThrowsAsync<PendingInstanceException>(() => _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset));
    }

    [Fact]
    public async Task GivenAnExistingDicomInstance_WhenAdded_ThenDicomInstanceAlreadyExistsExceptionShouldBeThrown()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

        long version = await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset);
        await _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, version);

        await Assert.ThrowsAsync<InstanceAlreadyExistsException>(() => _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset));
    }

    [Fact]
    public async Task GivenAnExistingDicomInstance_WhenStatusIsUpdated_ThenStatusShouldBeUpdated()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DicomDataset dataset = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid).Dataset;

        long version = await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset);

        Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);

        Assert.NotNull(instance);

        DateTimeOffset lastStatusUpdatedDate = instance.LastStatusUpdatedDate;

        // Make sure there is delay between.
        await Task.Delay(50);

        await _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, version);

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

        long version = await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset);

        VersionedInstanceIdentifier versionedInstanceIdentifier = new VersionedInstanceIdentifier(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                version);

        await _indexDataStore.DeleteInstanceIndexAsync(versionedInstanceIdentifier);

        await Assert.ThrowsAsync<InstanceNotFoundException>(
            () => _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, version));

        Assert.Empty(await _testHelper.GetInstancesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid));
    }

    [Fact]
    public async Task GivenADeletedDicomInstance_WhenIncrementingRetryCount_NewRetryCountShouldBeReturned()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();
        Instance instance1 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);

        DeletedInstance deletedEntry = (await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid)).First();
        var versionedDicomInstanceIdentifier = new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, deletedEntry.Watermark);
        var retryCount = await _indexDataStore.IncrementDeletedInstanceRetryAsync(versionedDicomInstanceIdentifier, DateTimeOffset.UtcNow);
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

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);

        string sopInstanceUid2 = TestUidGenerator.Generate();
        Instance instance2 = await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);

        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid2, DateTimeOffset.UtcNow);

        var numMatchRetryCount = await _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(0);
        Assert.Equal(2, numMatchRetryCount);
    }

    [Fact]
    public async Task GivenNoDeletedInstances_OldestDeletedIsCurrentTime()
    {
        await _testHelper.ClearDeletedInstanceTableAsync();

        Assert.InRange(await _indexDataStore.GetOldestDeletedAsync(), DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
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
        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid, DateTimeOffset.UtcNow);
        DeletedInstance first = (await _testHelper.GetDeletedInstanceEntriesAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid)).Single();

        // Create and delete another entry
        await Task.Delay(1000);

        string sopInstanceUid2 = TestUidGenerator.Generate();
        await CreateIndexAndVerifyInstance(studyInstanceUid, seriesInstanceUid, sopInstanceUid2);
        await _indexDataStore.DeleteInstanceIndexAsync(Partition.Default, studyInstanceUid, seriesInstanceUid, sopInstanceUid2, DateTimeOffset.UtcNow);
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
        await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset, QueryTagService.CoreQueryTags);
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
        long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(Partition.Default, dataset, queryTags);
        await Assert.ThrowsAsync<ExtendedQueryTagsOutOfDateException>(
            () => _indexDataStore.EndCreateInstanceIndexAsync(Partition.DefaultKey, dataset, watermark, queryTags, _defaultFileProperties));
    }

    [Fact]
    public Task GivenMultipleNewInstancesInSameStudy_WhenAddedInParallel_ThenItShouldBeAdded()
    {
        string studyInstanceUid = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();

        DicomDataset instance1 = CreateTestDicomDataset(studyInstanceUid, seriesInstanceUid);
        DicomDataset instance2 = CreateTestDicomDataset(studyInstanceUid, seriesInstanceUid);

        return Task.WhenAll(
            _indexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), instance1),
            _indexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), instance2));
    }


    [Fact]
    public Task GivenMultipleNewInstancesInDifferentStudy_SameSeriesAndSopInstanceUID_ThenItShouldBeAdded()
    {
        string studyInstanceUid1 = TestUidGenerator.Generate();
        string studyInstanceUid2 = TestUidGenerator.Generate();
        string seriesInstanceUid = TestUidGenerator.Generate();
        string sopInstanceUid = TestUidGenerator.Generate();

        DicomDataset instance1 = CreateTestDicomDataset(studyInstanceUid1, seriesInstanceUid, sopInstanceUid);
        DicomDataset instance2 = CreateTestDicomDataset(studyInstanceUid2, seriesInstanceUid, sopInstanceUid);

        return Task.WhenAll(
            _indexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), instance1),
            _indexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), instance2));
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

    private Action<DeletedInstance> ValidateSingleDeletedInstance(Instance instance, FileProperties expectedFileProperties = null)
    {
        return deletedInstance =>
        {
            Assert.Equal(instance.StudyInstanceUid, deletedInstance.StudyInstanceUid);
            Assert.Equal(instance.SeriesInstanceUid, deletedInstance.SeriesInstanceUid);
            Assert.Equal(instance.SopInstanceUid, deletedInstance.SopInstanceUid);
            Assert.Equal(instance.Watermark, deletedInstance.Watermark);
            Assert.InRange(deletedInstance.DeletedDateTime, _startDateTime.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
            Assert.Equal(0, deletedInstance.RetryCount);
            Assert.InRange(deletedInstance.CleanupAfter, _startDateTime.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
            if (expectedFileProperties != null)
            {
                Assert.Equal(expectedFileProperties.Path, deletedInstance.FilePath);
                Assert.Equal(expectedFileProperties.ETag, deletedInstance.ETag);
            }
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
        dataset.AddOrUpdate(DicomTag.PatientName, "pname");
        dataset.Add(DicomTag.ReferringPhysicianName, "rname");
        dataset.Add(DicomTag.StudyDate, "20200301");
        dataset.Add(DicomTag.StudyDescription, "sd");
        dataset.Add(DicomTag.AccessionNumber, "an");
        dataset.Add(DicomTag.Modality, "M");
        dataset.Add(DicomTag.PerformedProcedureStepStartDate, "20200302");
        return dataset;
    }

    private async Task<Instance> CreateIndexAndVerifyInstance(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, Partition partition = null, FileProperties filePropertiesToCreate = null)
    {
        partition ??= Partition.Default;
        DicomDataset dataset = CreateTestDicomDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        long version = await _indexDataStore.BeginCreateInstanceIndexAsync(partition, dataset);
        await _indexDataStore.EndCreateInstanceIndexAsync(partition.Key, dataset, version, filePropertiesToCreate);
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
