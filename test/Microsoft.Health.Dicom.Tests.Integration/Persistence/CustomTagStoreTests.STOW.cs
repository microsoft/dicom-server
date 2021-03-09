// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.SqlServer.Features.CustomTag;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Integration.Persistence.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    /// <summary>
    /// Tests for CustomTagStore
    /// </summary>
    public partial class CustomTagStoreTests : IClassFixture<SqlDataStoreTestsFixture>
    {
        [Fact]
        public async Task GivenDicomInstanceWithStudyLevelCustomTag_WhenStore_ThenCustomTagsNeedToBeAdded()
        {
            await ValidateAddNewCustomTagIndexData(CustomTagLevel.Study);
        }

        [Fact]
        public async Task GivenDicomInstanceWithSeriesLevelCustomTag_WhenStore_ThenCustomTagsNeedToBeAdded()
        {
            await ValidateAddNewCustomTagIndexData(CustomTagLevel.Series);
        }

        [Fact]
        public async Task GivenDicomInstanceWithInstanceLevelCustomTag_WhenStore_ThenCustomTagsNeedToBeAdded()
        {
            await ValidateAddNewCustomTagIndexData(CustomTagLevel.Instance);
        }

        [Fact]
        public async Task GivenDicomInstanceWithStudyLevelCustomTag_WhenStoreWithNewValue_ThenCustomTagsNeedToBeUpdated()
        {
            await ValidateUpdateExistingCustomTagIndexData(CustomTagLevel.Study);
        }

        [Fact]
        public async Task GivenDicomInstanceWithSeriesLevelCustomTag_WhenStoreWithNewValue_ThenCustomTagsNeedToBeUpdated()
        {
            await ValidateUpdateExistingCustomTagIndexData(CustomTagLevel.Series);
        }

        [Theory]
        [MemberData(nameof(SupportedTypes))]
        internal async Task GivenDicomInstanceWithDifferentTypeOfCustomTags_WhenStore_ThenTheyShouldBeStoredInProperTable(CustomTagDataType dataType, DicomElement element, object expectedValue)
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            dataset.Add(element);
            CustomTagLevel level = CustomTagLevel.Study;
            var customTagEntry = element.Tag.BuildCustomTagEntry(level: level);

            IndexTag indexTag = await AddCustomTag(customTagEntry);
            try
            {
                long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, new IndexTag[] { indexTag });
                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
                IReadOnlyList<CustomTagDataRow> rows = await _testHelper.GetCustomTagDataAsync(dataType, indexTag.CustomTagStoreEntry.Key, instance.StudyKey);
                Assert.Single(rows);
                Assert.Equal(watermark, rows[0].Watermark);
                Assert.Equal(expectedValue, rows[0].TagValue);
            }
            finally
            {
                await _customTagStore.DeleteCustomTagAsync(customTagEntry.Path, customTagEntry.VR);
            }
        }

        public static IEnumerable<object[]> SupportedTypes()
        {
            yield return new object[] { CustomTagDataType.StringData, new DicomCodeString(DicomTag.ConversionType, "STRING"), "STRING" };
            yield return new object[] { CustomTagDataType.DateTimeData, new DicomDateTime(DicomTag.AcquisitionDateTime, DateTime.Parse("2020/2/21")), DateTime.Parse("2020/2/21") };
            yield return new object[] { CustomTagDataType.DoubleData, new DicomFloatingPointDouble(DicomTag.DopplerCorrectionAngle, 1.0), 1.0 };
            yield return new object[] { CustomTagDataType.LongData, new DicomSignedLong(DicomTag.ReferencePixelX0, 1), 1L };
            yield return new object[] { CustomTagDataType.PersonNameData, new DicomPersonName(DicomTag.DistributionNameRETIRED, "abc^abc"), "abc^abc" };
        }

        private async Task ValidateAddNewCustomTagIndexData(CustomTagLevel level)
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();
            DicomDataset dataset = Samples.CreateRandomInstanceDataset(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            DicomTag tag = DicomTag.ConversionType;
            string value = "SYN";
            dataset.Add(tag, value);

            IndexTag indexTag = await AddCustomTag(tag.BuildCustomTagEntry(level: level));
            try
            {
                long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, new IndexTag[] { indexTag });
                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
                long? seriesKey = level != CustomTagLevel.Study ? instance.SeriesKey : null;
                long? instanceKey = level == CustomTagLevel.Instance ? instance.InstanceKey : null;
                var stringRows = await _testHelper.GetCustomTagDataAsync(CustomTagDataType.StringData, indexTag.CustomTagStoreEntry.Key, instance.StudyKey, seriesKey, instanceKey);

                Assert.Single(stringRows);
                Assert.Equal(stringRows[0].TagValue, value);
                Assert.Equal(stringRows[0].Watermark, watermark);
            }
            finally
            {
                await _customTagStore.DeleteCustomTagAsync(indexTag.CustomTagStoreEntry.Path, indexTag.CustomTagStoreEntry.VR);
            }
        }

        private async Task<IndexTag> AddCustomTag(CustomTagEntry customTagEntry)
        {
            return (await AddCustomTags(new CustomTagEntry[] { customTagEntry }))[0];
        }

        private async Task<IReadOnlyList<IndexTag>> AddCustomTags(IEnumerable<CustomTagEntry> customTags)
        {
            await _customTagStore.AddCustomTagsAsync(customTags);
            var customTagEntries = await _customTagStore.GetCustomTagsAsync();
            return customTagEntries.Select(entry => IndexTag.FromCustomTagStoreEntry(entry)).ToList();
        }

        private async Task ValidateUpdateExistingCustomTagIndexData(CustomTagLevel level)
        {
            if (level == CustomTagLevel.Instance)
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
            IndexTag indexTag = await AddCustomTag(tag.BuildCustomTagEntry(level: level));
            try
            {
                // index custom tags
                await _indexDataStore.CreateInstanceIndexAsync(dataset, new IndexTag[] { indexTag });

                // update
                value = "NEWSYN";
                dataset.AddOrUpdate(tag, value);
                sopInstanceUid = TestUidGenerator.Generate();
                dataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);
                if (level == CustomTagLevel.Study)
                {
                    seriesInstanceUid = TestUidGenerator.Generate();
                    dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesInstanceUid);
                }

                // index new instance
                long watermark = await _indexDataStore.CreateInstanceIndexAsync(dataset, new IndexTag[] { indexTag });
                Instance instance = await _testHelper.GetInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, watermark);
                long? seriesKey = level != CustomTagLevel.Study ? instance.SeriesKey : null;
                var stringRows = await _testHelper.GetCustomTagDataAsync(CustomTagDataType.StringData, indexTag.CustomTagStoreEntry.Key, instance.StudyKey, seriesKey);

                Assert.Single(stringRows);
                Assert.Equal(stringRows[0].TagValue, value);
                Assert.Equal(stringRows[0].Watermark, watermark);
            }
            finally
            {
                await _customTagStore.DeleteCustomTagAsync(indexTag.CustomTagStoreEntry.Path, indexTag.CustomTagStoreEntry.VR);
            }
        }
    }
}
