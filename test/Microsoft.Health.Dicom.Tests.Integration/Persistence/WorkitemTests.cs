// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class WorkitemTests : IClassFixture<SqlDataStoreTestsFixture>
{
    private readonly SqlDataStoreTestsFixture _fixture;

    public WorkitemTests(SqlDataStoreTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenValidWorkitemIsCreated_CreationSucceeds()
    {
        string workitemUid = DicomUID.Generate().UID;
        DicomTag tag2 = DicomTag.PatientName;

        var dataset = CreateSampleDataset(workitemUid, tag2);

        var queryTags = new List<QueryTag>()
        {
            new QueryTag(new WorkitemQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code)),
        };

        var identifier = await _fixture
            .IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(identifier);
        Assert.True(identifier.WorkitemKey > 0);

        await _fixture
            .IndexWorkitemStore
            .EndAddWorkitemAsync(Partition.DefaultKey, identifier.WorkitemKey, CancellationToken.None)
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task WhenValidWorkitemIsDeleted_DeletionSucceeds()
    {
        string workitemUid = DicomUID.Generate().UID;
        DicomTag tag2 = DicomTag.PatientName;

        var dataset = CreateSampleDataset(workitemUid, tag2);

        var queryTags = new List<QueryTag>()
        {
            new QueryTag(new WorkitemQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code)),
        };

        var identifier = await _fixture
            .IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        await _fixture.IndexWorkitemStore
            .DeleteWorkitemAsync(identifier, CancellationToken.None)
            .ConfigureAwait(false);

        // Try adding it back again, if this succeeds, then assume that Delete operation has succeeded.
        identifier = await _fixture
            .IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(identifier);
        Assert.True(identifier.WorkitemKey > 0);

        await _fixture.IndexWorkitemStore
            .DeleteWorkitemAsync(identifier, CancellationToken.None)
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task GivenIndexWorkitemStoreUpdateWorkitemProcedureStepStateAsync_SavesProcedureStepStateSuccessfully()
    {
        var workitemUid = DicomUID.Generate().UID;
        var (patientIdTag, patientIdTagKey) = (DicomTag.PatientID, 2);
        var (procedureStepStateTag, procedureStepStateTagKey) = (DicomTag.ProcedureStepState, 8);

        var dataset = CreateSampleDataset(workitemUid, patientIdTag);
        dataset.AddOrUpdate(procedureStepStateTag, ProcedureStepState.Scheduled.GetStringValue());

        var queryTags = new List<QueryTag>()
        {
            new QueryTag(new WorkitemQueryTagStoreEntry(patientIdTagKey, patientIdTag.GetPath(), patientIdTag.GetDefaultVR().Code)),
            new QueryTag(new WorkitemQueryTagStoreEntry(procedureStepStateTagKey, procedureStepStateTag.GetPath(), procedureStepStateTag.GetDefaultVR().Code)),
        };

        var identifier = await _fixture
            .IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        await _fixture.IndexWorkitemStore
            .EndAddWorkitemAsync(Partition.DefaultKey, identifier.WorkitemKey, CancellationToken.None)
            .ConfigureAwait(false);

        var workitemMetadata = await _fixture.IndexWorkitemStore
            .GetWorkitemMetadataAsync(Partition.DefaultKey, workitemUid, CancellationToken.None)
            .ConfigureAwait(false);

        (long CurrentWatermark, long NextWatermark)? result = await _fixture.IndexWorkitemStore
            .GetCurrentAndNextWorkitemWatermarkAsync(workitemMetadata.WorkitemKey, CancellationToken.None)
            .ConfigureAwait(false);

        var transactionUid = DicomUID.Generate().UID;
        await _fixture.IndexWorkitemStore
            .UpdateWorkitemProcedureStepStateAsync(workitemMetadata, result.Value.NextWatermark, ProcedureStepState.Canceled.GetStringValue(), transactionUid, CancellationToken.None)
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task WhenGetWorkitemQueryTagsIsExecuted_ReturnTagsSuccessfully()
    {
        var workitemQueryTags = await _fixture.IndexWorkitemStore
            .GetWorkitemQueryTagsAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotEmpty(workitemQueryTags);
    }

    [Fact]
    public async Task GivenGetWorkitemMetadataAsync_WhenWorkitemNotFound_ThenReturnsNull()
    {
        var workitemUid = DicomUID.Generate().UID;

        var workitemMetadata = await _fixture.IndexWorkitemStore
            .GetWorkitemMetadataAsync(Partition.DefaultKey, workitemUid, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Null(workitemMetadata);
    }

    [Fact]
    public async Task WhenWorkitemIsQueried_ThenReturnsMatchingWorkitems()
    {
        string workitemUid = DicomUID.Generate().UID;
        DicomTag tag = DicomTag.PatientID;

        var dataset = CreateSampleDataset(workitemUid, tag);

        var queryTags = new List<QueryTag>()
        {
            new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code)),
        };

        var identifier = await _fixture
            .IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        await _fixture.IndexWorkitemStore
            .EndAddWorkitemAsync(Partition.DefaultKey, identifier.WorkitemKey, CancellationToken.None)
            .ConfigureAwait(false);

        var includeField = new QueryIncludeField(new List<DicomTag> { tag });
        var queryTag = new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code));
        var filters = new List<QueryFilterCondition>()
        {
            new StringSingleValueMatchCondition(queryTag, "FOO"),
        };

        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);

        var result = await _fixture.IndexWorkitemStore
            .QueryAsync(Partition.DefaultKey, query, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.WorkitemInstances.Any());

        Assert.Equal(identifier.WorkitemKey, result.WorkitemInstances.FirstOrDefault().WorkitemKey);
    }

    [Fact]
    public async Task WhenWorkitemIsQueriedWithOffsetAndLimit_ThenReturnsMatchingWorkitems()
    {
        string workitemUid1 = DicomUID.Generate().UID;
        string workitemUid2 = DicomUID.Generate().UID;
        DicomTag tag = DicomTag.PatientID;

        var dataset1 = CreateSampleDataset(workitemUid1, tag);
        var dataset2 = CreateSampleDataset(workitemUid2, tag);

        var queryTags = new List<QueryTag>()
        {
            new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code)),
        };

        var identifier1 = await _fixture.IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset1, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        await _fixture.IndexWorkitemStore
            .EndAddWorkitemAsync(Partition.DefaultKey, identifier1.WorkitemKey, CancellationToken.None)
            .ConfigureAwait(false);

        var identifier2 = await _fixture.IndexWorkitemStore
            .BeginAddWorkitemAsync(Partition.DefaultKey, dataset2, queryTags, CancellationToken.None)
            .ConfigureAwait(false);

        await _fixture.IndexWorkitemStore
            .EndAddWorkitemAsync(Partition.DefaultKey, identifier2.WorkitemKey, CancellationToken.None)
            .ConfigureAwait(false);

        var includeField = new QueryIncludeField(new List<DicomTag> { tag });
        var queryTag = new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code));
        var filters = new List<QueryFilterCondition>()
        {
            new StringSingleValueMatchCondition(queryTag, "FOO"),
        };

        var query = new BaseQueryExpression(includeField, false, 1, 0, filters);

        var result = await _fixture.IndexWorkitemStore
            .QueryAsync(Partition.DefaultKey, query, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Single(result.WorkitemInstances);

        Assert.Equal(identifier2.WorkitemKey, result.WorkitemInstances.FirstOrDefault().WorkitemKey);

        query = new BaseQueryExpression(includeField, false, 1, 1, filters);

        result = await _fixture.IndexWorkitemStore
            .QueryAsync(Partition.DefaultKey, query, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Single(result.WorkitemInstances);

        Assert.Equal(identifier1.WorkitemKey, result.WorkitemInstances.FirstOrDefault().WorkitemKey);
    }

    private static DicomDataset CreateSampleDataset(string workitemUid, DicomTag tag)
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPInstanceUID, workitemUid },
            { tag, "FOO" }
        };
        return dataset;
    }
}
