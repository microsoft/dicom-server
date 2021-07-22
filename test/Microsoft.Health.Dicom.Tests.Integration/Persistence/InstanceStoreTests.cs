// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
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

        public InstanceStoreTests(SqlDataStoreTestsFixture fixture)
        {
            EnsureArg.IsNotNull(fixture?.ExtendedQueryTagStore, nameof(fixture.ExtendedQueryTagStore));
            EnsureArg.IsNotNull(fixture?.TestHelper, nameof(fixture.TestHelper));
            _instanceStore = fixture.InstanceStore;
            _indexDataStore = fixture.IndexDataStore;
            _extendedQueryTagStore = fixture.ExtendedQueryTagStore;
            _indexDataStoreTestHelper = fixture.TestHelper;
            EnsureArg.IsNotNull(fixture?.InstanceStore, nameof(fixture.InstanceStore));
            EnsureArg.IsNotNull(fixture?.IndexDataStore, nameof(fixture.IndexDataStore));
            _instanceStore = fixture.InstanceStore;
            _indexDataStore = fixture.IndexDataStore;
        }

        [Fact]
        public async Task GivenInstances_WhenGetInstanceIdentifiersByWatermarkRange_ThenItShouldReturnInstancesInRange()
        {
            var instance0 = await AddRandomInstanceAsync();
            var instance1 = await AddRandomInstanceAsync();
            var instance2 = await AddRandomInstanceAsync();
            var instance3 = await AddRandomInstanceAsync();
            var instance4 = await AddRandomInstanceAsync();
            var instances = await _instanceStore.GetInstanceIdentifiersByWatermarkRangeAsync(new WatermarkRange(instance1.Version, instance3.Version), IndexStatus.Creating);
            Assert.Equal(instances, new[] { instance1, instance2, instance3 });
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
            Instance instance1 = await CreateInstanceIndexAsync(dataset1, IndexStatus.Created);
            Instance instance2 = await CreateInstanceIndexAsync(dataset2, IndexStatus.Created);

            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Study));
            QueryTag queryTag = new QueryTag(tagStoreEntry);

            await _indexDataStore.ReindexInstanceAsync(dataset1, new[] { queryTag });
            await _indexDataStore.ReindexInstanceAsync(dataset2, new[] { queryTag });

            var row = (await _indexDataStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance1.StudyKey, null, null)).First();
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
            Instance instance1 = await CreateInstanceIndexAsync(dataset1, IndexStatus.Created);
            Instance instance2 = await CreateInstanceIndexAsync(dataset2, IndexStatus.Created);

            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Series));
            QueryTag queryTag = new QueryTag(tagStoreEntry);

            await _indexDataStore.ReindexInstanceAsync(dataset2, new[] { queryTag });
            await _indexDataStore.ReindexInstanceAsync(dataset1, new[] { queryTag });

            var row = (await _indexDataStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, null)).First();
            Assert.Equal(tagValue2, row.TagValue);
        }

        [Fact]
        public async Task GivenInstanceTag_WhenReindexWithNotIndexedInstance_ThenTagValueShouldBeInserted()
        {
            DicomTag tag = DicomTag.AcquisitionDeviceProcessingDescription;
            string tagValue = "test";

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            dataset.Add(tag, tagValue);

            Instance instance = await CreateInstanceIndexAsync(dataset, IndexStatus.Created);

            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            await _indexDataStore.ReindexInstanceAsync(dataset, new[] { new QueryTag(tagStoreEntry) });

            var row = (await _indexDataStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance.StudyKey, instance.SeriesKey, instance.InstanceKey)).First();
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
            var instance = await CreateInstanceIndexAsync(dataset, IndexStatus.Created);

            await _indexDataStore.ReindexInstanceAsync(dataset, new[] { new QueryTag(tagStoreEntry) });

            var row = (await _indexDataStoreTestHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, tagStoreEntry.Key, instance.StudyKey, instance.SeriesKey, instance.InstanceKey)).First();
            Assert.Equal(tagValue, row.TagValue);

        }

        [Fact]
        public async Task GivenInstanceNotExist_WhenReindex_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceID;
            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
            await Assert.ThrowsAsync<InstanceNotFoundException>(() => _indexDataStore.ReindexInstanceAsync(dataset, new[] { new QueryTag(tagStoreEntry) }));
        }

        [Fact]
        public async Task GivenPendingInstance_WhenReindex_ThenShouldThrowException()
        {
            DicomTag tag = DicomTag.DeviceDescription;
            var tagStoreEntry = await AddExtendedQueryTagAsync(tag.BuildAddExtendedQueryTagEntry(level: QueryTagLevel.Instance));

            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            await CreateInstanceIndexAsync(dataset, IndexStatus.Creating);
            await Assert.ThrowsAsync<PendingInstanceException>(() => _indexDataStore.ReindexInstanceAsync(dataset, new[] { new QueryTag(tagStoreEntry) }));
        }


        private async Task<ExtendedQueryTagStoreEntry> AddExtendedQueryTagAsync(AddExtendedQueryTagEntry addExtendedQueryTagEntry)
        {
            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(new[] { addExtendedQueryTagEntry }, 128);
            return (await _extendedQueryTagStore.GetExtendedQueryTagsAsync(path: addExtendedQueryTagEntry.Path)).First();
        }

        private async Task<Instance> CreateInstanceIndexAsync(DicomDataset dataset, IndexStatus indexStatus)
        {
            string studyUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);
            long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset);
            if (indexStatus != IndexStatus.Creating)
            {
                await _indexDataStore.UpdateInstanceIndexStatusAsync(new VersionedInstanceIdentifier(studyUid, seriesUid, sopInstanceUid, watermark), IndexStatus.Created);
            }

            return await _indexDataStoreTestHelper.GetInstanceAsync(studyUid, seriesUid, sopInstanceUid, watermark);
        }

        private async Task<VersionedInstanceIdentifier> AddRandomInstanceAsync()
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();

            string studyInstanceUid = dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dataset.GetString(DicomTag.SOPInstanceUID);

            long version = await _indexDataStore.CreateInstanceIndexAsync(dataset);
            return new VersionedInstanceIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version);
        }

    }
}
