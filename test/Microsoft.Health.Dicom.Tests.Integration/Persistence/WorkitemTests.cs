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
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemUid);
            dataset.Add(DicomTag.PatientName, "Foo");

            var queryTags = new List<QueryTag>()
            {
                new QueryTag(new WorkitemQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code)),
            };

            long workitemKey = await _fixture.IndexWorkitemStore.AddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);

            Assert.True(workitemKey > 0);
        }

        [Fact]
        public async Task WhenValidWorkitemIsDeleted_DeletionSucceeds()
        {
            string workitemUid = DicomUID.Generate().UID;
            DicomTag tag2 = DicomTag.PatientName;

            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemUid);
            dataset.Add(DicomTag.PatientName, "Foo");

            var queryTags = new List<QueryTag>()
            {
                new QueryTag(new WorkitemQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code)),
            };

            long workitemKey = await _fixture.IndexWorkitemStore.AddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);

            await _fixture.IndexWorkitemStore.DeleteWorkitemAsync(DefaultPartition.Key, workitemUid, CancellationToken.None);

            // Try adding it back again, if this succeeds, then assume that Delete operation has succeeded.
            workitemKey = await _fixture.IndexWorkitemStore.AddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);
            Assert.True(workitemKey > 0);

            await _fixture.IndexWorkitemStore.DeleteWorkitemAsync(DefaultPartition.Key, workitemUid, CancellationToken.None);
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

            var dataset = new DicomDataset();
            dataset.Add(DicomTag.AffectedSOPInstanceUID, workitemUid);
            dataset.Add(tag, "FOO");

            var queryTags = new List<QueryTag>()
            {
                new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code)),
            };

            long workitemKey = await _fixture.IndexWorkitemStore.AddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);

            var includeField = new QueryIncludeField(new List<DicomTag> { tag });
            var queryTag = new QueryTag(new WorkitemQueryTagStoreEntry(2, tag.GetPath(), tag.GetDefaultVR().Code));
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(queryTag, "FOO"),
            };

            var query = new QueryExpression(QueryResource.WorkitemInstances, includeField, false, 0, 0, filters, Array.Empty<string>());

            var result = await _fixture.IndexWorkitemStore.QueryAsync(DefaultPartition.Key, query, CancellationToken.None);

            Assert.True(result.WorkitemInstances.Any());

            Assert.Equal(workitemKey, result.WorkitemInstances.FirstOrDefault().WorkitemKey);
        }
    }
}
