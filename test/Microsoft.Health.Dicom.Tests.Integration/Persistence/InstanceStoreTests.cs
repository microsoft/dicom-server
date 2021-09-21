// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    ///  Tests for InstanceStore.
    /// </summary>
    public partial class InstanceStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        private readonly IInstanceStore _instanceStore;
        private readonly IIndexDataStore _indexDataStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IIndexDataStoreTestHelper _indexDataStoreTestHelper;
        private readonly IExtendedQueryTagStoreTestHelper _extendedQueryTagStoreTestHelper;

        public InstanceStoreTests(SqlDataStoreTestsFixture fixture)
        {
            _instanceStore = EnsureArg.IsNotNull(fixture?.InstanceStore, nameof(fixture.InstanceStore));
            _indexDataStore = EnsureArg.IsNotNull(fixture?.IndexDataStore, nameof(fixture.IndexDataStore));
            _extendedQueryTagStore = EnsureArg.IsNotNull(fixture?.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            _indexDataStoreTestHelper = EnsureArg.IsNotNull(fixture?.IndexDataStoreTestHelper, nameof(fixture.IndexDataStoreTestHelper));
            _extendedQueryTagStoreTestHelper = EnsureArg.IsNotNull(fixture?.ExtendedQueryTagStoreTestHelper, nameof(fixture.ExtendedQueryTagStoreTestHelper));
        }

        [Fact]
        public async Task GivenInstances_WhenGetInstanceIdentifiersByWatermarkRange_ThenItShouldReturnInstancesInRange()
        {
            await AddRandomInstanceAsync();
            var instance1 = await AddRandomInstanceAsync();
            var instance2 = await AddRandomInstanceAsync();
            var instance3 = await AddRandomInstanceAsync();
            var instance4 = await AddRandomInstanceAsync();
            await AddRandomInstanceAsync();

            IReadOnlyList<VersionedInstanceIdentifier> instances = await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(
                new WatermarkRange(instance1.Version, instance4.Version),
                IndexStatus.Creating);

            Assert.Equal(instances.OrderBy(x => x.Version), new[] { instance1, instance2, instance3, instance4 });
        }

        [Fact]
        public async Task GivenStudyTag_WhenReindexWithNewInstance_ThenTagValueShouldBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            string tagValue1 = "test1";
            string tagValue2 = "test2";

            string studyUid = TestUidGenerator.Generate();

            DicomDataset dataset1 = Samples.CreateRandomInstanceDataset(studyUid);
            dataset1.Add(tag, tagValue1);
            DicomDataset dataset2 = Samples.CreateRandomInstanceDataset(studyUid);
            dataset2.Add(tag, tagValue2);
            Instance instance1 = await CreateInstanceIndexAsync(dataset1);
            Instance instance2 = await CreateInstanceIndexAsync(dataset2);

            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Study));
            QueryTag queryTag = new QueryTag(tagStoreEntry);

            await _indexDataStore.ReindexInstanceAsync(dataset1, instance1.Watermark, new[] { queryTag });
            await _indexDataStore.ReindexInstanceAsync(dataset2, instance2.Watermark, new[] { queryTag });

            var row = (await _extendedQueryTagStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance1.StudyKey, null, null)).First();
            Assert.Equal(tagValue2, row.TagValue);
        }

        [Fact]
        public async Task GivenSeriesTag_WhenReindexWithOldInstance_ThenTagValueShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.AcquisitionDeviceProcessingCode;
            string tagValue1 = "test1";
            string tagValue2 = "test2";

            string studyUid = TestUidGenerator.Generate();
            string seriesUid = TestUidGenerator.Generate();

            DicomDataset dataset1 = Samples.CreateRandomInstanceDataset(studyUid, seriesUid);
            dataset1.Add(tag, tagValue1);
            DicomDataset dataset2 = Samples.CreateRandomInstanceDataset(studyUid, seriesUid);
            dataset2.Add(tag, tagValue2);
            Instance instance1 = await CreateInstanceIndexAsync(dataset1);
            Instance instance2 = await CreateInstanceIndexAsync(dataset2);

            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Series));
            QueryTag queryTag = new QueryTag(tagStoreEntry);

            await _indexDataStore.ReindexInstanceAsync(dataset2, instance2.Watermark, new[] { queryTag });
            await _indexDataStore.ReindexInstanceAsync(dataset1, instance1.Watermark, new[] { queryTag });

            var row = (await _extendedQueryTagStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, null)).First();
            Assert.Equal(tagValue2, row.TagValue);
        }

        [Fact]
        public async Task GivenInstanceTag_WhenReindexWithNotIndexedInstance_ThenTagValueShouldBeInserted()
        {
            DicomTag tag = DicomTag.AcquisitionDeviceProcessingDescription;
            string tagValue = "test";

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, tagValue);

            Instance instance = await CreateInstanceIndexAsync(dataset);

            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            await _indexDataStore.ReindexInstanceAsync(dataset, instance.Watermark, new[] { new QueryTag(tagStoreEntry) });

            var row = (await _extendedQueryTagStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance.StudyKey, instance.SeriesKey, instance.InstanceKey)).First();
            Assert.Equal(tagValue, row.TagValue);
        }

        [Fact]
        public async Task GivenInstanceTag_WhenReindexWithIndexedInstance_ThenTagValueShouldNotBeUpdated()
        {
            DicomTag tag = DicomTag.DeviceLabel;
            string tagValue = "test";
            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, tagValue);
            var instance = await CreateInstanceIndexAsync(dataset);

            await _indexDataStore.ReindexInstanceAsync(dataset, instance.Watermark, new[] { new QueryTag(tagStoreEntry) });

            var row = (await _extendedQueryTagStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance.StudyKey, instance.SeriesKey, instance.InstanceKey)).First();
            Assert.Equal(tagValue, row.TagValue);

        }

        [Fact]
        public async Task GivenInstanceNotExist_WhenReindex_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceID;
            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _indexDataStore.ReindexInstanceAsync(dataset, 0, new[] { new QueryTag(tagStoreEntry) }));
        }

        [Fact]
        public async Task GivenPendingInstance_WhenReindex_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceDescription;
            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);
            await Assert.ThrowsAsync<PendingInstanceException>(() => _indexDataStore.ReindexInstanceAsync(dataset, watermark, new[] { new QueryTag(tagStoreEntry) }));
        }

        [Fact]
        public async Task GivenInstances_WhenGettingInstanceBatches_ThenStartAtEnd()
        {
            var instances = new List<VersionedInstanceIdentifier>
            {
                await AddRandomInstanceAsync(),
                await AddRandomInstanceAsync(),
                await AddRandomInstanceAsync(),
                await AddRandomInstanceAsync(),
                await AddRandomInstanceAsync(),
                await AddRandomInstanceAsync(), // Deleted
                await AddRandomInstanceAsync(),
                await AddRandomInstanceAsync(),
            };

            // Create a gap within the data
            await _indexDataStore.DeleteInstanceIndexAsync(
                new InstanceIdentifier(
                    instances[^3].StudyInstanceUid,
                    instances[^3].SeriesInstanceUid,
                    instances[^3].SopInstanceUid));

            IReadOnlyList<WatermarkRange> batches;

            // No Max Watermark
            batches = await _instanceStore.GetInstanceBatchesAsync(3, 2, IndexStatus.Creating);

            Assert.Equal(2, batches.Count);
            Assert.Equal(new WatermarkRange(instances[^4].Version, instances[^1].Version), batches[0]);
            Assert.Equal(new WatermarkRange(instances[^7].Version, instances[^5].Version), batches[1]);

            // With Max Watermark
            batches = await _instanceStore.GetInstanceBatchesAsync(3, 2, IndexStatus.Creating, instances[^2].Version);

            Assert.Equal(2, batches.Count);
            Assert.Equal(new WatermarkRange(instances[^5].Version, instances[^2].Version), batches[0]);
            Assert.Equal(new WatermarkRange(instances[^8].Version, instances[^6].Version), batches[1]);
        }

        private async Task<ExtendedQueryTagStoreEntry> AddExtendedQueryTagAsync(AddExtendedQueryTagEntry addExtendedQueryTagEntry)
            => (await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new[] { addExtendedQueryTagEntry }, 128))[0];

        private async Task<Instance> CreateInstanceIndexAsync(DicomDataset dataset)
        {
            string studyUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);
            long watermark = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);
            await _indexDataStore.EndCreateInstanceIndexAsync(dataset, watermark);

            return await _indexDataStoreTestHelper.GetInstanceAsync(studyUid, seriesUid, sopInstanceUid, watermark);
        }

        private async Task<VersionedInstanceIdentifier> AddRandomInstanceAsync()
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);

            long version = await _indexDataStore.BeginCreateInstanceIndexAsync(dataset);
            return new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);
        }
    }
}
