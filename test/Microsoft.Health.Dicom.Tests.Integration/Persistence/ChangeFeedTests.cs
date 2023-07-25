// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

[Collection("Change Feed Collection")]
public class ChangeFeedTests : IClassFixture<ChangeFeedTestsFixture>
{
    private readonly ChangeFeedTestsFixture _fixture;
    private const string FilePath = "/svc/1/TestFile.dcm";

    public ChangeFeedTests(ChangeFeedTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenInstance_WhenAddedAndDeletedAndAdded_ChangeFeedEntryAvailable()
    {
        // create and validate
        var dicomInstanceIdentifier = await CreateInstanceAsync();
        await ValidateInsertFeedAsync(dicomInstanceIdentifier, 1);

        // delete and validate
        await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(Partition.DefaultKey, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
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
        await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(Partition.DefaultKey, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
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
        await _fixture.DicomIndexDataStore.DeleteInstanceIndexAsync(Partition.DefaultKey, dicomInstanceIdentifier.StudyInstanceUid, dicomInstanceIdentifier.SeriesInstanceUid, dicomInstanceIdentifier.SopInstanceUid, DateTime.Now, CancellationToken.None);
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
        FileProperties fileProperties = null)
    {
        var newDataSet = new DicomDataset()
        {
            { DicomTag.StudyInstanceUID, studyInstanceUid ?? TestUidGenerator.Generate() },
            { DicomTag.SeriesInstanceUID, seriesInstanceUid ?? TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, sopInstanceUid ?? TestUidGenerator.Generate() },
            { DicomTag.PatientID, TestUidGenerator.Generate() },
        };

        var version = await _fixture.DicomIndexDataStore.BeginCreateInstanceIndexAsync(new Partition(1, "clinic-one"), newDataSet);

        var versionedIdentifier = newDataSet.ToVersionedInstanceIdentifier(version, Partition.Default);

        if (instanceFullyCreated)
        {
            await _fixture.DicomIndexDataStore.EndCreateInstanceIndexAsync(1, newDataSet, version, fileProperties);
        }

        return versionedIdentifier;
    }
}
