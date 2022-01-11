// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partition;
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
                new QueryTag(new ExtendedQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled,0)),
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
            dataset.Add(DicomTag.SOPInstanceUID, workitemUid);
            dataset.Add(DicomTag.PatientName, "Foo");

            var queryTags = new List<QueryTag>()
            {
                new QueryTag(new ExtendedQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled,0)),
            };

            long workitemKey = await _fixture.IndexWorkitemStore.AddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);

            await _fixture.IndexWorkitemStore.DeleteWorkitemAsync(DefaultPartition.Key, workitemUid, CancellationToken.None);

            // Try adding it back again, if this succeeds, then assume that Delete operation has succeeded.
            workitemKey = await _fixture.IndexWorkitemStore.AddWorkitemAsync(DefaultPartition.Key, dataset, queryTags, CancellationToken.None);
            Assert.True(workitemKey > 0);

            await _fixture.IndexWorkitemStore.DeleteWorkitemAsync(DefaultPartition.Key, workitemUid, CancellationToken.None);
        }
    }
}
