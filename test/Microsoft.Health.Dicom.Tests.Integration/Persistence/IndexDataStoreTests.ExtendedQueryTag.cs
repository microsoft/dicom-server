// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Core;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for IndexDataStore
    /// </summary>
    public partial class IndexDataStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        [Fact]
        public async Task GivenDicomInstanceWithStudyLevelExtendedQueryTag_WhenStore_ThenExtendedQueryTagsNeedToBeAdded()
        {
            await ValidateAddNewExtendedQueryTagIndexData(QueryTagLevel.Study);
        }

        [Fact]
        public async Task GivenDicomInstanceWithSeriesLevelExtendedQueryTag_WhenStore_ThenExtendedQueryTagsNeedToBeAdded()
        {
            await ValidateAddNewExtendedQueryTagIndexData(QueryTagLevel.Series);
        }

        [Fact]
        public async Task GivenDicomInstanceWithInstanceLevelExtendedQueryTag_WhenStore_ThenExtendedQueryTagsNeedToBeAdded()
        {
            await ValidateAddNewExtendedQueryTagIndexData(QueryTagLevel.Instance);
        }

        [Fact]
        public async Task GivenDicomInstanceWithStudyLevelExtendedQueryTag_WhenStoreWithNewValue_ThenExtendedQueryTagsNeedToBeUpdated()
        {
            await ValidateUpdateExistingExtendedQueryTagIndexData(QueryTagLevel.Study);
        }

        [Fact]
        public async Task GivenDicomInstanceWithSeriesLevelExtendedQueryTag_WhenStoreWithNewValue_ThenExtendedQueryTagsNeedToBeUpdated()
        {
            await ValidateUpdateExistingExtendedQueryTagIndexData(QueryTagLevel.Series);
        }

        [Theory]
        [MemberData(nameof(SupportedTypes))]
        internal async Task GivenDicomInstanceWithDifferentTypeOfExtendedQueryTags_WhenStore_ThenTheyShouldBeStoredInProperTable(ExtendedQueryTagDataType dataType, DicomElement element, object expectedValue)
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            dataset.Add(element);
            QueryTagLevel level = QueryTagLevel.Study;
            var extendedQueryTagEntry = element.Tag.BuildExtendedQueryTagStoreEntry(level: level);

            QueryTag queryTag = await AddExtendedQueryTag(extendedQueryTagEntry);
            try
            {
                long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });
                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
                IReadOnlyList<ExtendedQueryTagDataRow> rows = await _testHelper.GetExtendedQueryTagDataAsync(dataType, queryTag.ExtendedQueryTagStoreEntry.Key, instance.StudyKey);
                Assert.Single(rows);
                Assert.Equal(watermark, rows[0].Watermark);
                Assert.Equal(expectedValue, rows[0].TagValue);
            }
            finally
            {
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(extendedQueryTagEntry.Path, extendedQueryTagEntry.VR);
            }
        }

        [Fact]
        public async Task GivenDicomInstanceWithDifferentTypesOfExtendedQueryTags_WhenDeletedBySopInstanceId_ThenTagValuesShouldBeRemoved()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();
            try
            {
                // Store 5 tags, 1 study level datetime tag, 2 series level string and double tags and 2 instance level long and person name tags.
                IReadOnlyList<QueryTag> tags = await StoreTagsOfSuportedDataTypes();

                // Create 2 instances in same series and same study.
                Instance instance1 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid, sopInstanceUid, 1, tags);
                Instance instance2 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid, sopInstanceUid2, 2, tags);

                var queryTags = tags.ToArray();

                // Delete by instance uid.
                await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

                // Study and series level tags should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DateTimeData, queryTags[0].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTags[1].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DoubleData, queryTags[2].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey));

                // Instance level tags under the deleted instance should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance1.InstanceKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance1.InstanceKey));

                // Instance level tags under the other instance should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance2.InstanceKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance2.InstanceKey));
            }
            finally
            {
                await CleanupExtendedQueryTags();
            }
        }

        [Fact]
        public async Task GivenDicomInstancesWithDifferentTypesOfExtendedQueryTags_WhenDeletedBySeriesId_ThenTagValuesShouldBeRemoved()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid2 = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();
            string sopInstanceUid3 = TestUidGenerator.Generate();

            try
            {
                // Store 5 tags, 1 study level datetime tag, 2 series level string and double tags and 2 instance level long and person name tags.
                IReadOnlyList<QueryTag> tags = await StoreTagsOfSuportedDataTypes();

                // Create 2 instances in same series and 1 instance in different series all under the same study.
                Instance instance1 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid, sopInstanceUid, 1, tags);
                Instance instance2 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid, sopInstanceUid2, 2, tags);
                Instance instance3 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid2, sopInstanceUid3, 3, tags);

                var queryTags = tags.ToArray();

                // Delete by first series uid.
                await _indexDataStore.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, Clock.UtcNow);

                // Study level tags should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DateTimeData, queryTags[0].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey));

                // Series level tags under the first series should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTags[1].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DoubleData, queryTags[2].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey));

                // Instance level tags under the first instance in the deleted series should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance1.InstanceKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance1.InstanceKey));

                // Instance level tags under the second instance in the deleted series should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance2.InstanceKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance2.InstanceKey));

                // Series level tags under the second series should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTags[1].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance3.SeriesKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DoubleData, queryTags[2].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance3.SeriesKey));

                // Instance level tags under the instance in the second series should not be deleted
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance3.SeriesKey, instance3.InstanceKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance3.SeriesKey, instance3.InstanceKey));
            }
            finally
            {
                await CleanupExtendedQueryTags();
            }
        }

        [Fact]
        public async Task GivenDicomInstanceWithDifferentTypesOfExtendedQueryTags_WhenDeletedByStudyId_ThenTagValuesShouldBeRemoved()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string studyInstanceUid2 = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid2 = TestUidGenerator.Generate();
            string seriesInstanceUid3 = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid2 = TestUidGenerator.Generate();
            string sopInstanceUid3 = TestUidGenerator.Generate();

            try
            {
                // Store 5 tags, 1 study level datetime tag, 2 series level string and double tags and 2 instance level long and person name tags.
                IReadOnlyList<QueryTag> tags = await StoreTagsOfSuportedDataTypes();

                // Create 2 instances in different series but same study and 1 instance in different study
                Instance instance1 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid, sopInstanceUid, 1, tags);
                Instance instance2 = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid2, sopInstanceUid2, 2, tags);
                Instance instance3 = await StoreInstanceWithDifferentTagValues(studyInstanceUid2, seriesInstanceUid3, sopInstanceUid3, 3, tags);

                var queryTags = tags.ToArray();

                // Delete by first study uid.
                await _indexDataStore.DeleteStudyIndexAsync(studyInstanceUid, Clock.UtcNow);

                // Study level query tags for the first study should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DateTimeData, queryTags[0].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey));

                // Series level query tags for the first series under the first study should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTags[1].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DoubleData, queryTags[2].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey));

                // Instance level query tags for the first instance under the first series under the deleted study should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance1.InstanceKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance1.SeriesKey, instance1.InstanceKey));

                // Series level query tags for the first instance under the first series under the deleted study should be deleted.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance2.SeriesKey, instance2.InstanceKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance1.StudyKey, instance2.SeriesKey, instance2.InstanceKey));

                // Study level query tags for the second study should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DateTimeData, queryTags[0].ExtendedQueryTagStoreEntry.Key, instance3.StudyKey));

                // Series level query tags for the series under the second study should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance3.StudyKey, instance3.SeriesKey, instance3.InstanceKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance3.StudyKey, instance3.SeriesKey, instance3.InstanceKey));

                // Instance level query tags for the instance under the series under the second study should not be deleted.
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance3.StudyKey, instance3.SeriesKey, instance3.InstanceKey));
                Assert.Single(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance3.StudyKey, instance3.SeriesKey, instance3.InstanceKey));
            }
            finally
            {
                await CleanupExtendedQueryTags();
            }
        }

        [Fact]
        public async Task GivenDicomInstanceWithDifferentTypesOfExtendedQueryTags_WhenDeletedBySopInstanceId_ThenCascadingTagValuesShouldBeRemoved()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            try
            {
                // Store 5 tags, 1 study level datetime tag, 2 series level string and double tags and 2 instance level long and person name tags.
                IReadOnlyList<QueryTag> tags = await StoreTagsOfSuportedDataTypes();

                // Store only one instance.
                Instance instance = await StoreInstanceWithDifferentTagValues(studyInstanceUid, seriesInstanceUid, sopInstanceUid, 1, tags);

                var queryTags = tags.ToArray();

                // Delete by instance uid
                await _indexDataStore.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, Clock.UtcNow);

                // Ensure all tags regardless of level are removed as it is the only instance in series/study.
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DateTimeData, queryTags[0].ExtendedQueryTagStoreEntry.Key, instance.StudyKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTags[1].ExtendedQueryTagStoreEntry.Key, instance.StudyKey, instance.SeriesKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.DoubleData, queryTags[2].ExtendedQueryTagStoreEntry.Key, instance.StudyKey, instance.SeriesKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.LongData, queryTags[3].ExtendedQueryTagStoreEntry.Key, instance.StudyKey, instance.SeriesKey, instance.InstanceKey));
                Assert.Empty(await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.PersonNameData, queryTags[4].ExtendedQueryTagStoreEntry.Key, instance.StudyKey, instance.SeriesKey, instance.InstanceKey));
            }
            finally
            {
                await CleanupExtendedQueryTags();
            }
        }

        public static IEnumerable<object[]> SupportedTypes()
        {
            yield return new object[] { ExtendedQueryTagDataType.DateTimeData, new DicomDate(DicomTag.AcquisitionDate, DateTime.Parse("2020/2/21")), DateTime.Parse("2020/2/21") };
            yield return new object[] { ExtendedQueryTagDataType.StringData, new DicomCodeString(DicomTag.ConversionType, "STRING"), "STRING" };
            yield return new object[] { ExtendedQueryTagDataType.DoubleData, new DicomFloatingPointDouble(DicomTag.DopplerCorrectionAngle, 1.0), 1.0 };
            yield return new object[] { ExtendedQueryTagDataType.LongData, new DicomSignedLong(DicomTag.ReferencePixelX0, 1), 1L };
            yield return new object[] { ExtendedQueryTagDataType.PersonNameData, new DicomPersonName(DicomTag.DistributionNameRETIRED, "abc^abc"), "abc^abc" };
        }

        private async Task ValidateAddNewExtendedQueryTagIndexData(QueryTagLevel level)
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            DicomTag tag = DicomTag.ConversionType;
            string value = "SYN";
            dataset.Add(tag, value);

            QueryTag queryTag = await AddExtendedQueryTag(tag.BuildExtendedQueryTagStoreEntry(level: level));
            try
            {
                long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });
                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
                long? seriesKey = level != QueryTagLevel.Study ? instance.SeriesKey : null;
                long? instanceKey = level == QueryTagLevel.Instance ? instance.InstanceKey : null;
                var stringRows = await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTag.ExtendedQueryTagStoreEntry.Key, instance.StudyKey, seriesKey, instanceKey);

                Assert.Single(stringRows);
                Assert.Equal(stringRows[0].TagValue, value);
                Assert.Equal(stringRows[0].Watermark, watermark);
            }
            finally
            {
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(queryTag.ExtendedQueryTagStoreEntry.Path, queryTag.ExtendedQueryTagStoreEntry.VR);
            }
        }

        private async Task<QueryTag> AddExtendedQueryTag(ExtendedQueryTagStoreEntry extendedQueryTagEntry)
        {
            return (await AddExtendedQueryTags(new ExtendedQueryTagStoreEntry[] { extendedQueryTagEntry }))[0];
        }

        private async Task<IReadOnlyList<QueryTag>> AddExtendedQueryTags(IEnumerable<ExtendedQueryTagStoreEntry> extendedQueryTags)
        {
            await _extendedQueryTagStore.AddExtendedQueryTagsAsync(extendedQueryTags);
            var extendedQueryTagEntries = await _extendedQueryTagStore.GetExtendedQueryTagsAsync();
            return extendedQueryTagEntries.Select(entry => new QueryTag(entry)).ToList();
        }

        private async Task ValidateUpdateExistingExtendedQueryTagIndexData(QueryTagLevel level)
        {
            if (level == QueryTagLevel.Instance)
            {
                throw new System.ArgumentException("Update value on instance level is not valid case.");
            }

            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            DicomTag tag = DicomTag.ConversionType;
            string value = "SYN";
            dataset.Add(tag, value);
            QueryTag queryTag = await AddExtendedQueryTag(tag.BuildExtendedQueryTagStoreEntry(level: level));
            try
            {
                // index extended query tags
                await _indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });

                // update
                value = "NEWSYN";
                dataset.AddOrUpdate(tag, value);
                sopInstanceUid = TestUidGenerator.Generate();
                dataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);
                if (level == QueryTagLevel.Study)
                {
                    seriesInstanceUid = TestUidGenerator.Generate();
                    dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesInstanceUid);
                }

                // index new instance
                long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, new QueryTag[] { queryTag });
                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
                long? seriesKey = level != QueryTagLevel.Study ? instance.SeriesKey : null;
                var stringRows = await _testHelper.GetExtendedQueryTagDataAsync(ExtendedQueryTagDataType.StringData, queryTag.ExtendedQueryTagStoreEntry.Key, instance.StudyKey, seriesKey);

                Assert.Single(stringRows);
                Assert.Equal(stringRows[0].TagValue, value);
                Assert.Equal(stringRows[0].Watermark, watermark);
            }
            finally
            {
                await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(queryTag.ExtendedQueryTagStoreEntry.Path, queryTag.ExtendedQueryTagStoreEntry.VR);
            }
        }

        private async Task<IReadOnlyList<QueryTag>> StoreTagsOfSuportedDataTypes()
        {
            // Store 5 tags, 1 study level datetime tag, 2 series level string and double tags and 2 instance level long and person name tags.
            QueryTagLevel study = QueryTagLevel.Study;
            QueryTagLevel series = QueryTagLevel.Series;
            QueryTagLevel instance = QueryTagLevel.Instance;

            IEnumerable<ExtendedQueryTagStoreEntry> extendedQueryTagEntries = new List<ExtendedQueryTagStoreEntry>()
            {
                DicomTag.AcquisitionDate.BuildExtendedQueryTagStoreEntry(level: study),
                DicomTag.ConversionType.BuildExtendedQueryTagStoreEntry(level: series),
                DicomTag.DopplerCorrectionAngle.BuildExtendedQueryTagStoreEntry(level: series),
                DicomTag.ReferencePixelX0.BuildExtendedQueryTagStoreEntry(level: instance),
                DicomTag.DistributionNameRETIRED.BuildExtendedQueryTagStoreEntry(level: instance),
            };

            return await AddExtendedQueryTags(extendedQueryTagEntries);
        }

        private async Task<Instance> StoreInstanceWithDifferentTagValues(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int index, IReadOnlyList<QueryTag> queryTags)
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            dataset.Add(new DicomDate(DicomTag.AcquisitionDate, DateTime.Parse("2020/2/2" + index)));
            dataset.Add(new DicomCodeString(DicomTag.ConversionType, "STRING" + index));
            dataset.Add(new DicomFloatingPointDouble(DicomTag.DopplerCorrectionAngle, 1.0 + index));
            dataset.Add(new DicomSignedLong(DicomTag.ReferencePixelX0, 1 + index));
            dataset.Add(new DicomPersonName(DicomTag.DistributionNameRETIRED, "abc^abc" + index));

            long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, queryTags);
            return await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
        }

        private async Task CleanupExtendedQueryTags()
        {
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(DicomTag.AcquisitionDate.GetPath(), DicomTag.AcquisitionDate.GetDefaultVR().Code);
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(DicomTag.ConversionType.GetPath(), DicomTag.ConversionType.GetDefaultVR().Code);
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(DicomTag.DopplerCorrectionAngle.GetPath(), DicomTag.DopplerCorrectionAngle.GetDefaultVR().Code);
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(DicomTag.ReferencePixelX0.GetPath(), DicomTag.ReferencePixelX0.GetDefaultVR().Code);
            await _extendedQueryTagStore.DeleteExtendedQueryTagAsync(DicomTag.DistributionNameRETIRED.GetPath(), DicomTag.DistributionNameRETIRED.GetDefaultVR().Code);
        }
    }
}
