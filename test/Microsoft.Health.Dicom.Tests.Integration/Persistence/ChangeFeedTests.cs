// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

[Collection("Change Feed Collection")]
public class ChangeFeedTests : IClassFixture<ChangeFeedTestsFixture>
{
    private readonly IInstanceStore _instanceStore;
    private readonly ChangeFeedTestsFixture _fixture;
    private const string FilePath = "/svc/1/TestFile.dcm";
    private readonly IIndexDataStore _indexDataStore;
    private readonly IIndexDataStoreTestHelper _indexDataStoreTestHelper;
    private readonly IQueryStore _queryStore;

    public ChangeFeedTests(ChangeFeedTestsFixture fixture)
    {
        _fixture = fixture;
        _instanceStore = EnsureArg.IsNotNull(fixture?.InstanceStore, nameof(fixture.InstanceStore));
        _indexDataStore = EnsureArg.IsNotNull(fixture?.IndexDataStore, nameof(fixture.IndexDataStore));
        _indexDataStoreTestHelper = EnsureArg.IsNotNull(fixture?.IndexDataStoreTestHelper, nameof(fixture.IndexDataStoreTestHelper));
        _queryStore = EnsureArg.IsNotNull(fixture?.QueryStore, nameof(fixture.QueryStore));
    }

    [Fact]
    public async Task GivenInstance_WhenAddedAndDeletedAndAdded_ChangeFeedEntryAvailable()
    {
        // create and validate
        var dicomInstanceIdentifier = await CreateInstanceAsync();
        await ValidateInsertFeedAsync(dicomInstanceIdentifier, 1);

        // delete and validate
        await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(Partition.Default, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
        await ValidateDeleteFeedAsync(dicomInstanceIdentifier, 2);

        // re-create the same instance and validate
        await CreateInstanceAsync(true, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid);
        await ValidateInsertFeedAsync(dicomInstanceIdentifier, 3);
    }

    [Fact]
    public async Task GivenInstance_WhenSavedWithFileProperties_ChangeFeedEntryFilePathAvailable()
    {
        // create and validate
        FileProperties expectedFileProperties = new FileProperties
        {
            ETag = Guid.NewGuid().ToString(),
            Path = FilePath,
        };
        var dicomInstanceIdentifier = await CreateInstanceAsync(fileProperties: expectedFileProperties);
        await ValidateInsertFeedAsync(dicomInstanceIdentifier, 1, expectedFileProperties);

        // delete and validate - file properties are null on deletes
        await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(Partition.Default, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
        await ValidateDeleteFeedAsync(dicomInstanceIdentifier, 2, expectedFileProperties);

        // re-create the same instance without properties and validate properties are still null
        await CreateInstanceAsync(true, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid);
        await ValidateInsertFeedAsync(dicomInstanceIdentifier, 3, expectedFileProperties: null);
    }

    [Fact]
    public async Task GivenCreatingInstance_WhenDeleted_ValidateNoChangeFeedRecord()
    {
        // create and validate
        var dicomInstanceIdentifier = await CreateInstanceAsync(instanceFullyCreated: false);
        await ValidateNoChangeFeedAsync(dicomInstanceIdentifier);

        // delete and validate
        await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(Partition.Default, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
        await ValidateNoChangeFeedAsync(dicomInstanceIdentifier);
    }

    [Fact]
    public async Task GivenRecords_WhenQueryWithWindows_ThenScopeResults()
    {
        // Insert data over time
        // Note: There may be clock skew between the SQL server and the DICOM server,
        // so we'll try to account for it by waiting a bit and searching for the proper SQL server time
        await Task.Delay(1000);
        VersionedInstanceIdentifier instance1 = await CreateInstanceAsync();
        await Task.Delay(500);
        VersionedInstanceIdentifier instance2 = await CreateInstanceAsync();
        VersionedInstanceIdentifier instance3 = await CreateInstanceAsync();
        await Task.Delay(1000);

        ChangeFeedEntry first = await FindFirstChangeOrDefaultAsync(instance1, TimeSpan.FromMinutes(5));
        Assert.NotNull(first);

        // Get all creation events
        TimeRange testRange = TimeRange.After(first.Timestamp);
        IReadOnlyList<ChangeFeedEntry> changes = await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(testRange, 0, 10, ChangeFeedOrder.Time);
        Assert.Equal(3, changes.Count);
        Assert.Equal(instance1.Version, changes[0].CurrentVersion);
        Assert.Equal(instance2.Version, changes[1].CurrentVersion);
        Assert.Equal(instance3.Version, changes[2].CurrentVersion);

        // Fetch changes outside of the range
        IReadOnlyList<ChangeFeedEntry> existingEvents = await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(TimeRange.Before(changes[0].Timestamp), 0, 100, ChangeFeedOrder.Time);
        Assert.DoesNotContain(existingEvents, x => changes.Any(y => y.Sequence == x.Sequence));

        Assert.Empty(await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(TimeRange.After(changes[1].Timestamp), 2, 100, ChangeFeedOrder.Time));
        Assert.Empty(await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(TimeRange.After(changes[2].Timestamp.AddMilliseconds(1)), 0, 100, ChangeFeedOrder.Time));

        // Fetch changes limited to window
        await ValidateSubsetAsync(testRange, changes[0], changes[1], changes[2]);
        await ValidateSubsetAsync(new TimeRange(changes[0].Timestamp, changes[2].Timestamp), changes[0], changes[1]);
    }

    [Fact]
    public async Task GivenInstances_WhenBulkUpdateInstancesInAStudyAndExternalStoreDisabled_ThenItShouldUpdateChangeFeedWithNullPath()
    {
        var studyInstanceUID1 = TestUidGenerator.Generate();
        VersionedInstanceIdentifier instanceVersionedIdentifier1 = await CreateUpdateInstances(studyInstanceUID1);

        var instance1 = await _indexDataStoreTestHelper.GetInstanceAsync(instanceVersionedIdentifier1.StudyInstanceUid, instanceVersionedIdentifier1.SeriesInstanceUid, instanceVersionedIdentifier1.SopInstanceUid, instanceVersionedIdentifier1.Version);

        PartitionModel partitionModel = await _indexDataStoreTestHelper.GetPartitionAsync(instance1.PartitionKey);

        // Update the instances with newWatermark
        await _indexDataStore.BeginUpdateInstancesAsync(new Partition((int)partitionModel.Key, partitionModel.Name), studyInstanceUID1);

        var dicomDataset = new DicomDataset();
        dicomDataset.AddOrUpdate(DicomTag.PatientName, "FirstName_NewLastName");

        await _indexDataStore.EndUpdateInstanceAsync(Partition.DefaultKey, studyInstanceUID1, dicomDataset, new List<InstanceMetadata>());

        var instanceMetadataList = (await _instanceStore.GetInstanceIdentifierWithPropertiesAsync(Partition.Default, studyInstanceUID1)).ToList();

        // Verify if the new patient name is updated
        var result = await _queryStore.GetStudyResultAsync(Partition.DefaultKey, new[] { instanceMetadataList.First().VersionedInstanceIdentifier.Version });

        Assert.True(result.Any());
        Assert.Equal("FirstName_NewLastName", result.First().PatientName);

        // Verify Changefeed entries are inserted
        var changeFeedEntries = await _fixture.IndexDataStoreTestHelper.GetUpdatedChangeFeedRowsAsync(4);
        Assert.True(changeFeedEntries.Any());
        for (int i = 0; i < instanceMetadataList.Count; i++)
        {
            Assert.Equal(instanceMetadataList[i].VersionedInstanceIdentifier.Version, changeFeedEntries[i].OriginalWatermark);
            Assert.Equal(instanceMetadataList[i].VersionedInstanceIdentifier.Version, changeFeedEntries[i].CurrentWatermark);
            Assert.Equal(instanceMetadataList[i].VersionedInstanceIdentifier.SopInstanceUid, changeFeedEntries[i].SopInstanceUid);
            // when no file properties passed in and not using external store, change feed file path remains null
            Assert.Null(changeFeedEntries[i].FilePath);
        }
    }

    [Fact]
    public async Task GivenInstances_WhenBulkUpdateInstancesInAStudyAndFilePropertiesPassedIn_ThenItShouldAlsoInsertFilePathOnChangeFeed()
    {
        var studyInstanceUID1 = TestUidGenerator.Generate();
        VersionedInstanceIdentifier instanceVersionedIdentifier1 = await CreateUpdateInstances(studyInstanceUID1);

        var instance1 = await _indexDataStoreTestHelper.GetInstanceAsync(instanceVersionedIdentifier1.StudyInstanceUid, instanceVersionedIdentifier1.SeriesInstanceUid, instanceVersionedIdentifier1.SopInstanceUid, instanceVersionedIdentifier1.Version);

        // Update the instances with newWatermark
        IReadOnlyList<InstanceMetadata> updatedInstanceMetadata = await _indexDataStore.BeginUpdateInstancesAsync(
            new Partition(instance1.PartitionKey, Partition.UnknownName),
            studyInstanceUID1);

        // generate file property per updated instance with new watermark
        Dictionary<long, InstanceMetadata> metadataListByWatermark = CreateInstanceMetadataListWithFileProperties(updatedInstanceMetadata);

        var dicomDataset = new DicomDataset();
        dicomDataset.AddOrUpdate(DicomTag.PatientName, "FirstName_NewLastName");

        await _indexDataStore.EndUpdateInstanceAsync(Partition.DefaultKey, studyInstanceUID1, dicomDataset, metadataListByWatermark.Values.ToList());

        var instanceMetadataList = (await _instanceStore.GetInstanceIdentifierWithPropertiesAsync(Partition.Default, studyInstanceUID1)).ToList();

        // Verify Changefeed entries had file path updated
        foreach (var instanceMetadata in instanceMetadataList)
        {
            var changeFeedEntries = await _fixture.IndexDataStoreTestHelper.GetChangeFeedRowsAsync(
                instanceMetadata.VersionedInstanceIdentifier.StudyInstanceUid,
                instanceMetadata.VersionedInstanceIdentifier.SeriesInstanceUid,
                instanceMetadata.VersionedInstanceIdentifier.SopInstanceUid);
            Assert.True(changeFeedEntries.Any());
            foreach (ChangeFeedRow row in changeFeedEntries)
            {
                metadataListByWatermark.TryGetValue(row.CurrentWatermark.Value, out var actual);
                Assert.Equal(actual.InstanceProperties.fileProperties.Path, row.FilePath);
            }
        }
    }

    private static Dictionary<long, InstanceMetadata> CreateInstanceMetadataListWithFileProperties(IReadOnlyList<InstanceMetadata> instanceMetadataList)
    {
        Dictionary<long, InstanceMetadata> updatedInstanceMetadata = new Dictionary<long, InstanceMetadata>();
        foreach (var updatedInstance in instanceMetadataList)
        {
            updatedInstanceMetadata.Add(updatedInstance.InstanceProperties.NewVersion.Value, new InstanceMetadata(
                updatedInstance.VersionedInstanceIdentifier,
                new InstanceProperties
                {
                    fileProperties = new FileProperties
                    {
                        Path = $"test/file_{updatedInstance.InstanceProperties.NewVersion.Value}.dcm",
                        ETag = $"etag_{updatedInstance.InstanceProperties.NewVersion.Value}"
                    },
                    NewVersion = updatedInstance.InstanceProperties.NewVersion,
                    OriginalVersion = updatedInstance.InstanceProperties.OriginalVersion
                }));
        }

        return updatedInstanceMetadata;
    }

    private async Task<ChangeFeedEntry> FindFirstChangeOrDefaultAsync(InstanceIdentifier identifier, TimeSpan duration, int limit = 200)
    {
        int offset = 0;
        IReadOnlyList<ChangeFeedEntry> changes;
        DateTimeOffset start = DateTimeOffset.UtcNow.Add(-duration);

        do
        {
            changes = await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(TimeRange.After(start), offset, limit, ChangeFeedOrder.Time);
            ChangeFeedEntry change = changes.FirstOrDefault(x =>
                x.StudyInstanceUid == identifier.StudyInstanceUid &&
                x.SeriesInstanceUid == identifier.SeriesInstanceUid &&
                x.SopInstanceUid == identifier.SopInstanceUid);

            if (change != null)
                return change;

            offset += limit;
        } while (changes.Count == limit);

        return null;
    }

    private async Task ValidateInsertFeedAsync(VersionedInstanceIdentifier dicomInstanceIdentifier, int expectedCount, FileProperties expectedFileProperties = null)
    {
        IReadOnlyList<ChangeFeedRow> result = await _fixture.DicomIndexDataStoreTestHelper.GetChangeFeedRowsAsync(
            dicomInstanceIdentifier.StudyInstanceUid,
            dicomInstanceIdentifier.SeriesInstanceUid,
            dicomInstanceIdentifier.SopInstanceUid);

        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
        Assert.Equal((int)ChangeFeedAction.Create, result.Last().Action);
        Assert.Equal(result.Last().OriginalWatermark, result.Last().CurrentWatermark);
        Assert.Equal(expectedFileProperties?.Path, result.Last().FilePath);

        int i = 0;
        while (i < expectedCount - 1)
        {
            ChangeFeedRow r = result[i];
            Assert.NotEqual(r.OriginalWatermark, r.CurrentWatermark);
            i++;
        }
    }

    private async Task ValidateDeleteFeedAsync(VersionedInstanceIdentifier dicomInstanceIdentifier, int expectedCount, FileProperties expectedFileProperties = null)
    {
        IReadOnlyList<ChangeFeedRow> result = await _fixture.DicomIndexDataStoreTestHelper.GetChangeFeedRowsAsync(
            dicomInstanceIdentifier.StudyInstanceUid,
            dicomInstanceIdentifier.SeriesInstanceUid,
            dicomInstanceIdentifier.SopInstanceUid);

        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
        Assert.Equal((int)ChangeFeedAction.Delete, result.Last().Action);

        foreach (ChangeFeedRow row in result)
        {
            Assert.Null(row.CurrentWatermark);
            if (row.Action == (int)ChangeFeedAction.Create)
            {
                Assert.Equal(expectedFileProperties?.Path, row.FilePath);
            }
            else
            {
                // DELETE change feed entry should not have a file path
                Assert.Null(row.FilePath);
            }
        }
    }

    private async Task ValidateNoChangeFeedAsync(VersionedInstanceIdentifier dicomInstanceIdentifier)
    {
        IReadOnlyList<ChangeFeedRow> result = await _fixture.DicomIndexDataStoreTestHelper.GetChangeFeedRowsAsync(
            dicomInstanceIdentifier.StudyInstanceUid,
            dicomInstanceIdentifier.SeriesInstanceUid,
            dicomInstanceIdentifier.SopInstanceUid);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private async Task ValidateSubsetAsync(TimeRange range, params ChangeFeedEntry[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            IReadOnlyList<ChangeFeedEntry> changes = await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(range, i, 1, ChangeFeedOrder.Time);

            Assert.Single(changes);
            Assert.Equal(expected[i].Sequence, changes.Single().Sequence);
        }

        Assert.Empty(await _fixture.DicomChangeFeedStore.GetChangeFeedAsync(range, expected.Length, 1, ChangeFeedOrder.Time));
    }

    private async Task<VersionedInstanceIdentifier> CreateInstanceAsync(
        bool instanceFullyCreated = true,
        string studyInstanceUid = null,
        string seriesInstanceUid = null,
        string sopInstanceUid = null,
        bool createFileProperties = false,
        FileProperties fileProperties = null,
        DicomDataset dataset = null,
        Partition partition = null)
    {
        dataset ??= CreateRandomDataSet(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        partition ??= Partition.Default;

        var version = await _fixture.DicomIndexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), dataset);
        var versionedIdentifier = dataset.ToVersionedInstanceIdentifier(version, Partition.Default);

        fileProperties ??= CreateFileProperties(createFileProperties, version);
        if (instanceFullyCreated)
        {
            await _fixture.DicomIndexDataStore.EndCreateInstanceIndexAsync(partition.Key, dataset, version, fileProperties);
        }

        return versionedIdentifier;
    }

    private async Task<VersionedInstanceIdentifier> CreateUpdateInstances(string studyInstanceUID1)
    {
        DicomDataset dataset1 = Samples.CreateRandomInstanceDataset(studyInstanceUID1);
        DicomDataset dataset2 = Samples.CreateRandomInstanceDataset(studyInstanceUID1);
        DicomDataset dataset3 = Samples.CreateRandomInstanceDataset(studyInstanceUID1);
        DicomDataset dataset4 = Samples.CreateRandomInstanceDataset(studyInstanceUID1);
        dataset4.AddOrUpdate(DicomTag.PatientName, "FirstName_LastName");

        var instanceVersionedIdentifier1 = await CreateInstanceAsync(dataset: dataset1);
        await CreateInstanceAsync(dataset: dataset2);
        await CreateInstanceAsync(dataset: dataset3);
        await CreateInstanceAsync(dataset: dataset4);
        return instanceVersionedIdentifier1;
    }

    private static DicomDataset CreateRandomDataSet(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        return new DicomDataset()
        {
            { DicomTag.StudyInstanceUID, studyInstanceUid ?? TestUidGenerator.Generate() },
            { DicomTag.SeriesInstanceUID, seriesInstanceUid ?? TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, sopInstanceUid ?? TestUidGenerator.Generate() },
            { DicomTag.PatientID, TestUidGenerator.Generate() },
        };
    }

    private static FileProperties CreateFileProperties(bool createFileProperty, long watermark)
    {
        if (createFileProperty)
        {
            return new FileProperties
            {
                Path = $"test/file_{watermark}.dcm",
                ETag = $"etag_{watermark}",
            };
        }
        return null;
    }
}
