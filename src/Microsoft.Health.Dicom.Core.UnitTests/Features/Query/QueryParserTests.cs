// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class QueryParserTests
    {
        private readonly QueryParser _queryParser;

        public QueryParserTests()
        {
            _queryParser = new QueryParser(new DicomTagParser());
        }

        [Fact]
        public void GivenParameters_WhenParsing_ThenForwardValues()
        {
            var parameters = new QueryParameters
            {
                Filters = new Dictionary<string, string>(),
                FuzzyMatching = true,
                IncludeField = Array.Empty<string>(),
                Limit = 12,
                Offset = 700,
            };

            QueryExpression actual = _queryParser.Parse(parameters, Array.Empty<QueryTag>());
            Assert.Equal(parameters.FuzzyMatching, actual.FuzzyMatching);
            Assert.Equal(parameters.Limit, actual.Limit);
            Assert.Equal(parameters.Offset, actual.Offset);
        }

        [Theory]
        [InlineData("StudyDate")]
        [InlineData("00100020")]
        [InlineData("00100020,00100010")]
        [InlineData("StudyDate,StudyTime")]
        public void GivenIncludeField_WithValidAttributeId_CheckIncludeFields(string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            VerifyIncludeFieldsForValidAttributeIds(value.Split(','));
        }

        [Fact]
        public void GivenIncludeField_WithValueAll_CheckAllValue()
        {
            QueryExpression queryExpression = _queryParser.Parse(
                CreateParameters(new Dictionary<string, string>(), QueryResource.AllStudies, includeField: new string[] { "all" }),
                QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.IncludeFields.All);
        }

        [Fact]
        public void GivenIncludeField_WithInvalidAttributeId_Throws()
        {
            Assert.Throws<QueryParseException>(() => _queryParser.Parse(
                CreateParameters(new Dictionary<string, string>(), QueryResource.AllStudies, includeField: new string[] { "something" }),
                QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("12050010")]
        [InlineData("12051001")]
        public void GivenIncludeField_WithPrivateAttributeId_CheckIncludeFields(string value)
        {
            VerifyIncludeFieldsForValidAttributeIds(value);
        }

        [Theory]
        [InlineData("includefield", "12345678")]
        [InlineData("includefield", "98765432")]
        public void GivenIncludeField_WithUnknownAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("00100010", "joe")]
        [InlineData("PatientName", "joe")]
        public void GivenFilterCondition_ValidTag_CheckProperties(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.HasFilters);
            var singleValueCond = queryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(singleValueCond);
            Assert.True(singleValueCond.QueryTag.Tag == DicomTag.PatientName);
            Assert.True(singleValueCond.Value == value);
        }

        [Theory]
        [InlineData("ReferringPhysicianName", "dr^joe")]
        public void GivenFilterCondition_ValidReferringPhysicianNameTag_CheckProperties(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.HasFilters);
            var singleValueCond = queryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(singleValueCond);
            Assert.Equal(DicomTag.ReferringPhysicianName, singleValueCond.QueryTag.Tag);
            Assert.Equal(value, singleValueCond.Value);
        }

        [Theory]
        [InlineData("00080061", "CT")]
        public void GivenFilterCondition_WithNotSupportedTag_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("Modality", "CT", QueryResource.AllStudies)]
        [InlineData("SOPInstanceUID", "1.2.3.48898989", QueryResource.AllSeries)]
        [InlineData("PatientName", "Joe", QueryResource.StudySeries)]
        [InlineData("Modality", "CT", QueryResource.StudySeriesInstances)]
        public void GivenFilterCondition_WithKnownTagButNotSupportedAtLevel_Throws(string key, string value, QueryResource resourceType)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), resourceType), QueryTagService.CoreQueryTags));
        }

        [Fact]
        public void GivenExtendedQueryPrivateTag_WithUrl_ParseSucceeds()
        {
            DicomTag tag = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            QueryTag queryTag = new QueryTag(tag.BuildExtendedQueryTagStoreEntry(vr: DicomVRCode.CS, level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton(tag.GetPath(), "Test"), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDateTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.Date.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("Date", "19510910-20200220"), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDateTimeTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("DateTime", "20200301195109.10-20200501195110.20"), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Theory]
        [InlineData("19510910010203", "20200220020304")]
        public void GivenDateTime_WithValidRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(minValue));
            EnsureArg.IsNotNull(maxValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("DateTime", string.Concat(minValue, "-", maxValue)), QueryResource.AllStudies), new[] { queryTag });
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.QueryTag.Tag == DicomTag.DateTime);
            Assert.True(cond.Minimum == DateTime.ParseExact(minValue, QueryParser.DateTimeTagValueFormats, null));
            Assert.True(cond.Maximum == DateTime.ParseExact(maxValue, QueryParser.DateTimeTagValueFormats, null));
        }

        [Theory]
        [InlineData("", "20200220020304")]
        [InlineData("19510910010203", "")]
        public void GivenDateTime_WithEmptyMinOrMaxValueInRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(minValue));
            EnsureArg.IsNotNull(maxValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("DateTime", string.Concat(minValue, "-", maxValue)), QueryResource.AllStudies), new[] { queryTag });
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(DicomTag.DateTime, cond.QueryTag.Tag);

            DateTime expectedMin = string.IsNullOrEmpty(minValue) ? DateTime.MinValue : DateTime.ParseExact(minValue, QueryParser.DateTimeTagValueFormats, null);
            DateTime expectedMax = string.IsNullOrEmpty(maxValue) ? DateTime.MaxValue : DateTime.ParseExact(maxValue, QueryParser.DateTimeTagValueFormats, null);
            Assert.Equal(expectedMin, cond.Minimum);
            Assert.Equal(expectedMax, cond.Maximum);
        }

        [Fact]
        public void GivenDateTime_WithEmptyMinAndMaxInRangeMatch_Throw()
        {
            QueryTag queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton("DateTime", "-"), QueryResource.AllStudies), new[] { queryTag }));
        }

        [Fact]
        public void GivenExtendedQueryTimeTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.Time.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("Time", "195109.10-195110.20"), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Theory]
        [InlineData("010203", "020304")]
        public void GivenStudyTime_WithValidRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(minValue));
            EnsureArg.IsNotNull(maxValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(DicomTag.Time.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("Time", string.Concat(minValue, "-", maxValue)), QueryResource.AllStudies), new[] { queryTag });
            var cond = queryExpression.FilterConditions.First() as LongRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(DicomTag.Time, cond.QueryTag.Tag);

            long minTicks = new DicomTime(cond.QueryTag.Tag, new string[] { minValue }).Get<DateTime>().Ticks;
            long maxTicks = new DicomTime(cond.QueryTag.Tag, new string[] { maxValue }).Get<DateTime>().Ticks;

            Assert.Equal(minTicks, cond.Minimum);
            Assert.Equal(maxTicks, cond.Maximum);
        }

        [Theory]
        [InlineData("", "020304")]
        [InlineData("010203", "")]
        public void GivenStudyTime_WithEmptyMinOrMaxValueInRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(minValue));
            EnsureArg.IsNotNull(maxValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(DicomTag.Time.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("Time", string.Concat(minValue, "-", maxValue)), QueryResource.AllStudies), new[] { queryTag });
            var cond = queryExpression.FilterConditions.First() as LongRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.QueryTag.Tag == DicomTag.Time);

            long minTicks = string.IsNullOrEmpty(minValue) ? 0 : new DicomTime(cond.QueryTag.Tag, new string[] { minValue }).Get<DateTime>().Ticks;
            long maxTicks = string.IsNullOrEmpty(maxValue) ? TimeSpan.TicksPerDay : new DicomTime(cond.QueryTag.Tag, new string[] { maxValue }).Get<DateTime>().Ticks;

            Assert.Equal(minTicks, cond.Minimum);
            Assert.Equal(maxTicks, cond.Maximum);
        }

        [Fact]
        public void GivenStudyTime_WithEmptyMinAndMaxInRangeMatch_Throw()
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton("StudyTime", "-"), QueryResource.AllSeries), QueryTagService.CoreQueryTags));
        }

        [Fact]
        public void GivenExtendedQueryPersonNameTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.PatientBirthName.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));

            QueryExpression queryExpression = _queryParser.Parse(
                CreateParameters(GetSingleton(nameof(DicomTag.PatientBirthName), "Joe"), QueryResource.AllSeries, fuzzyMatching: true),
                new[] { queryTag });
            var fuzzyCondition = queryExpression.FilterConditions.First() as PersonNameFuzzyMatchCondition;
            Assert.NotNull(fuzzyCondition);
            Assert.Equal("Joe", fuzzyCondition.Value);
            Assert.Equal(queryTag, fuzzyCondition.QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryStringTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton(nameof(DicomTag.ModelGroupUID), "abc"), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryStringTag_WithTagPathUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton("00687004", "abc"), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryLongTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.NumberOfAssessmentObservations.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton(nameof(DicomTag.NumberOfAssessmentObservations), "50"), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDoubleTag_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateParameters(GetSingleton(nameof(DicomTag.FloatingPointValue), "1.1"), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDoubleTagWithInvalidValue_WithUrl_ParseFails()
        {
            QueryTag queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(nameof(DicomTag.FloatingPointValue), "abc"), QueryResource.AllStudies), new[] { queryTag }));
        }

        [Fact]
        public void GivenNonExistingExtendedQueryStringTag_WithUrl_ParseFails()
        {
            QueryTag queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(nameof(DicomTag.ModelGroupUID), "abc"), QueryResource.AllStudies), new[] { queryTag }));
        }

        [Fact]
        public void GivenCombinationOfExtendedQueryAndStandardTags_WithUrl_ParseSucceeds()
        {
            QueryTag queryTag1 = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryTag queryTag2 = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(
                CreateParameters(
                    new Dictionary<string, string>
                    {
                        { "PatientName", "Joe" },
                        { "FloatingPointValue", "1.1" },
                        { "StudyDate", "19510910-20200220" },
                        { "00687004", "abc" },
                    },
                    QueryResource.AllSeries),
                QueryTagService.CoreQueryTags.Concat(new[] { queryTag1, queryTag2 }).ToList());
            Assert.Equal(4, queryExpression.FilterConditions.Count);
            Assert.Contains(queryTag1, queryExpression.FilterConditions.Select(x => x.QueryTag));
            Assert.Contains(queryTag2, queryExpression.FilterConditions.Select(x => x.QueryTag));
        }

        [Fact]
        public void GivenFilterCondition_WithDuplicateQueryParam_Throws()
        {
            Assert.Throws<QueryParseException>(() => _queryParser.Parse(
                CreateParameters(
                    new Dictionary<string, string>
                    {
                        { "PatientName", "Joe" },
                        { "00100010", "Rob" },
                    },
                    QueryResource.AllStudies),
                QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("PatientName", "  ")]
        [InlineData("StudyDescription", "")]
        public void GivenFilterCondition_WithInvalidAttributeIdStringValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("00390061", "invalidtag")]
        [InlineData("unkownparam", "invalidtag")]
        public void GivenFilterCondition_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("StudyDate", "19510910-20200220")]
        public void GivenStudyDate_WithValidRangeMatch_CheckCondition(string key, string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            QueryExpression queryExpression = _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(DicomTag.StudyDate, cond.QueryTag.Tag);
            Assert.Equal(DateTime.ParseExact(value.Split('-')[0], QueryParser.DateTagValueFormat, null), cond.Minimum);
            Assert.Equal(DateTime.ParseExact(value.Split('-')[1], QueryParser.DateTagValueFormat, null), cond.Maximum);
        }

        [Theory]
        [InlineData("StudyDate", "-20200220")]
        public void GivenStudyDate_WithEmptyMinValueInRangeMatch_CheckCondition(string key, string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            QueryExpression queryExpression = _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(DicomTag.StudyDate, cond.QueryTag.Tag);
            Assert.Equal(DateTime.MinValue, cond.Minimum);
            Assert.Equal(DateTime.ParseExact(value.Split('-')[1], QueryParser.DateTagValueFormat, null), cond.Maximum);
        }

        [Theory]
        [InlineData("StudyDate", "19510910-")]
        public void GivenStudyDate_WithEmptyMaxValueInRangeMatch_CheckCondition(string key, string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            QueryExpression queryExpression = _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(DicomTag.StudyDate, cond.QueryTag.Tag);
            Assert.Equal(DateTime.ParseExact(value.Split('-')[0], QueryParser.DateTagValueFormat, null), cond.Minimum);
            Assert.Equal(DateTime.MaxValue, cond.Maximum);
        }

        [Theory]
        [InlineData("StudyDate", "2020/02/28")]
        [InlineData("StudyDate", "20200230")]
        [InlineData("StudyDate", "20200228-20200230")]
        [InlineData("StudyDate", "20200110-20200109")]
        [InlineData("StudyDate", "-")]
        [InlineData("PerformedProcedureStepStartDate", "baddate")]
        public void GivenDateTag_WithInvalidDate_Throw(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value), QueryResource.AllSeries), QueryTagService.CoreQueryTags));
        }

        [Fact]
        public void GivenStudyInstanceUID_WithUrl_CheckFilterCondition()
        {
            var testStudyInstanceUid = TestUidGenerator.Generate();
            QueryExpression queryExpression = _queryParser
                .Parse(CreateParameters(new Dictionary<string, string>(), QueryResource.AllSeries, testStudyInstanceUid), QueryTagService.CoreQueryTags);
            Assert.Equal(1, queryExpression.FilterConditions.Count);
            var cond = queryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(testStudyInstanceUid, cond.Value);
        }

        [Fact]
        public void GivenPatientNameFilterCondition_WithFuzzyMatchingTrue_FuzzyMatchConditionAdded()
        {
            QueryExpression queryExpression = _queryParser.Parse(
                CreateParameters(
                    new Dictionary<string, string>
                    {
                        { "PatientName", "CoronaPatient" },
                        { "StudyDate", "20200403" },
                    },
                    QueryResource.AllStudies,
                    fuzzyMatching: true),
                QueryTagService.CoreQueryTags);

            Assert.Equal(2, queryExpression.FilterConditions.Count);

            var studyDateFilterCondition = queryExpression.FilterConditions.FirstOrDefault(c => c.QueryTag.Tag == DicomTag.StudyDate) as DateSingleValueMatchCondition;
            Assert.NotNull(studyDateFilterCondition);

            QueryFilterCondition patientNameCondition = queryExpression.FilterConditions.FirstOrDefault(c => c.QueryTag.Tag == DicomTag.PatientName);
            Assert.NotNull(patientNameCondition);

            var fuzzyCondition = patientNameCondition as PersonNameFuzzyMatchCondition;
            Assert.NotNull(fuzzyCondition);
            Assert.Equal("CoronaPatient", fuzzyCondition.Value);
        }

        [Fact]
        public void GivenErroneousTag_WhenParse_ThenShouldBeInList()
        {
            DicomTag tag1 = DicomTag.PatientAge;
            DicomTag tag2 = DicomTag.PatientAddress;
            QueryTag[] tags = new QueryTag[]
            {
              new QueryTag(new ExtendedQueryTagStoreEntry(1, tag1.GetPath(), tag1.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled,1)), // has error
              new QueryTag(new ExtendedQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled,0)), // no error
            };
            QueryExpression queryExpression = _queryParser.Parse(
                CreateParameters(
                    new Dictionary<string, string>
                    {
                        { tag1.GetFriendlyName(), "CoronaPatient" },
                        { tag2.GetPath(), "20200403" },
                    },
                    QueryResource.AllInstances),
                tags);

            Assert.Single(queryExpression.ErroneousTags);
            Assert.Equal(queryExpression.ErroneousTags.First(), tag1.GetFriendlyName());
        }

        [Fact]
        public void GivenDisabledTag_WhenParse_ThenShouldThrowException()
        {
            DicomTag tag1 = DicomTag.PatientAge;
            QueryTag[] tags = new QueryTag[]
            {
              new QueryTag(new ExtendedQueryTagStoreEntry(1, tag1.GetPath(), tag1.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Disabled,1)), // disabled
            };
            var parameters = CreateParameters(
                    new Dictionary<string, string>
                    {
                        { tag1.GetFriendlyName(), "CoronaPatient" },
                    },
                    QueryResource.AllInstances);

            var exp = Assert.Throws<QueryParseException>(() => _queryParser.Parse(parameters, tags));
            Assert.Equal($"Query is disabled on specified attribute '{tag1.GetFriendlyName()}'.", exp.Message);
        }

        private void VerifyIncludeFieldsForValidAttributeIds(params string[] values)
        {
            QueryExpression queryExpression = _queryParser.Parse(
                CreateParameters(new Dictionary<string, string>(), QueryResource.AllStudies, includeField: values),
                QueryTagService.CoreQueryTags);

            Assert.False(queryExpression.HasFilters);
            Assert.False(queryExpression.IncludeFields.All);
            Assert.Equal(values.Length, queryExpression.IncludeFields.DicomTags.Count);
        }

        private Dictionary<string, string> GetSingleton(string key, string value)
            => new Dictionary<string, string> { { key, value } };

        private QueryParameters CreateParameters(
            Dictionary<string, string> filters,
            QueryResource resourceType,
            string studyInstanceUid = null,
            string seriesInstanceUid = null,
            bool fuzzyMatching = false,
            string[] includeField = null)
        {
            return new QueryParameters
            {
                Filters = filters,
                FuzzyMatching = fuzzyMatching,
                IncludeField = includeField ?? Array.Empty<string>(),
                QueryResourceType = resourceType,
                SeriesInstanceUid = seriesInstanceUid,
                StudyInstanceUid = studyInstanceUid,
            };
        }
    }
}
