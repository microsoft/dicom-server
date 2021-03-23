// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
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
            var extendedQueryTagEntry = element.Tag.BuildExtendedQueryTagEntry(level: level);

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

            QueryTag queryTag = await AddExtendedQueryTag(tag.BuildExtendedQueryTagEntry(level: level));
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

        private async Task<QueryTag> AddExtendedQueryTag(ExtendedQueryTagEntry extendedQueryTagEntry)
        {
            return (await AddExtendedQueryTags(new ExtendedQueryTagEntry[] { extendedQueryTagEntry }))[0];
        }

        private async Task<IReadOnlyList<QueryTag>> AddExtendedQueryTags(IEnumerable<ExtendedQueryTagEntry> extendedQueryTags)
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
            QueryTag queryTag = await AddExtendedQueryTag(tag.BuildExtendedQueryTagEntry(level: level));
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
    }
}
