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
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
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

            var dataset = new DicomDataset();
            dataset.Add(DicomTag.SOPInstanceUID, workitemUid);
            dataset.Add(DicomTag.PatientName, "Foo");

            var queryTags = new List<QueryTag>()
            {
                new QueryTag(new WorkitemQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code)),
            };

            var identifier = await _fixture
                .IndexWorkitemStore
                .BeginAddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);

            Assert.NotNull(identifier);
            Assert.True(identifier.WorkitemKey > 0);

            await _fixture
                .IndexWorkitemStore
                .EndAddWorkitemAsync(DefaultPartition.Key, identifier.WorkitemKey, CancellationToken.None);
        }

        [Fact]
        public async Task WhenValidWorkitemIsDeleted_DeletionSucceeds()
        {
            string workitemUid = DicomUID.Generate().UID;
            DicomTag tag2 = DicomTag.PatientName;

            var dataset = new DicomDataset();
            dataset.Add(DicomTag.SOPInstanceUID, workitemUid);
            dataset.Add(DicomTag.PatientName, "Foo");

            var queryTags = new List<QueryTag>()
            {
                new QueryTag(new WorkitemQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code)),
            };

            var identifier = await _fixture
                .IndexWorkitemStore
                .BeginAddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None)
                .ConfigureAwait(false);

            await _fixture.IndexWorkitemStore.DeleteWorkitemAsync(identifier, CancellationToken.None);

            // Try adding it back again, if this succeeds, then assume that Delete operation has succeeded.
            identifier = await _fixture
                .IndexWorkitemStore
                .BeginAddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.NotNull(identifier);
            Assert.True(identifier.WorkitemKey > 0);

            await _fixture.IndexWorkitemStore.DeleteWorkitemAsync(identifier, CancellationToken.None);
        }

        [Fact]
        public async Task WhenGetWorkitemQueryTagsIsExecuted_ReturnTagsSuccessfully()
        {
            var workitemQueryTags = await _fixture.IndexWorkitemStore.GetWorkitemQueryTagsAsync(CancellationToken.None);

            Assert.NotEmpty(workitemQueryTags);
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
                .BeginAddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None)
                .ConfigureAwait(false);

            await _fixture.IndexWorkitemStore
                .EndAddWorkitemAsync(DefaultPartition.Key, identifier.WorkitemKey, CancellationToken.None);

            var includeField = new QueryIncludeField(new List<DicomTag> { tag });
            var queryTag = new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code));
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(queryTag, "FOO"),
            };

            var query = new BaseQueryExpression(includeField, false, 0, 0, filters);

            var result = await _fixture.IndexWorkitemStore.QueryAsync(DefaultPartition.Key, query, CancellationToken.None);

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

            var identifier1 = await _fixture.IndexWorkitemStore.BeginAddWorkitemAsync(DefaultPartition.Key, dataset1, queryTags, CancellationToken.None);
            await _fixture.IndexWorkitemStore
                .EndAddWorkitemAsync(DefaultPartition.Key, identifier1.WorkitemKey, CancellationToken.None);
            var identifier2 = await _fixture.IndexWorkitemStore.BeginAddWorkitemAsync(DefaultPartition.Key, dataset2, queryTags, CancellationToken.None);
            await _fixture.IndexWorkitemStore
                .EndAddWorkitemAsync(DefaultPartition.Key, identifier2.WorkitemKey, CancellationToken.None);

            var includeField = new QueryIncludeField(new List<DicomTag> { tag });
            var queryTag = new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code));
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(queryTag, "FOO"),
            };

            var query = new BaseQueryExpression(includeField, false, 1, 0, filters);

            var result = await _fixture.IndexWorkitemStore.QueryAsync(DefaultPartition.Key, query, CancellationToken.None);

            Assert.Single(result.WorkitemInstances);

            Assert.Equal(identifier2.WorkitemKey, result.WorkitemInstances.FirstOrDefault().WorkitemKey);

            query = new BaseQueryExpression(includeField, false, 1, 1, filters);

            result = await _fixture.IndexWorkitemStore.QueryAsync(DefaultPartition.Key, query, CancellationToken.None);

            Assert.Single(result.WorkitemInstances);

            Assert.Equal(identifier1.WorkitemKey, result.WorkitemInstances.FirstOrDefault().WorkitemKey);
        }

        private DicomDataset CreateSampleDataset(string workitemUid, DicomTag tag)
        {
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.SOPInstanceUID, workitemUid);
            dataset.Add(tag, "FOO");
            return dataset;
        }
    }
}
