// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;
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

        [Theory]
        [InlineData("includefield", "StudyDate")]
        [InlineData("includefield", "00100020")]
        [InlineData("includefield", "00100020,00100010")]
        [InlineData("includefield", "StudyDate, StudyTime")]
        public void GivenIncludeField_WithValidAttributeId_CheckIncludeFields(string key, string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            VerifyIncludeFieldsForValidAttributeIds(key, value);
        }

        [Theory]
        [InlineData("includefield", "all")]
        public void GivenIncludeField_WithValueAll_CheckAllValue(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.IncludeFields.All);
        }

        [Theory]
        [InlineData("includefield", "something")]
        public void GivenIncludeField_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("includefield", "12050010")]
        [InlineData("includefield", "12051001")]
        public void GivenIncludeField_WithPrivateAttributeId_CheckIncludeFields(string key, string value)
        {
            VerifyIncludeFieldsForValidAttributeIds(key, value);
        }

        [Theory]
        [InlineData("includefield", "12345678")]
        [InlineData("includefield", "98765432")]
        public void GivenIncludeField_WithUnknownAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("00100010", "joe")]
        [InlineData("PatientName", "joe")]
        public void GivenFilterCondition_ValidTag_CheckProperties(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
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
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
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
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("Modality", "CT", QueryResource.AllStudies)]
        [InlineData("SOPInstanceUID", "1.2.3.48898989", QueryResource.AllSeries)]
        [InlineData("PatientName", "Joe", QueryResource.StudySeries)]
        [InlineData("Modality", "CT", QueryResource.StudySeriesInstances)]
        public void GivenFilterCondition_WithKnownTagButNotSupportedAtLevel_Throws(string key, string value, QueryResource resourceType)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), resourceType), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("limit=25&offset=0&fuzzymatching=false&includefield=00081030,00080060&StudyDate=19510910-20200220", QueryResource.AllStudies)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&limit=50", QueryResource.AllStudies)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&Modality=CT", QueryResource.AllSeries)]
        [InlineData("ReferringPhysicianName=dr&fuzzyMatching=true&Modality=CT", QueryResource.AllSeries)]
        [InlineData("PatientBirthDate=18000101-19010101", QueryResource.AllStudies)]
        [InlineData("ManufacturerModelName=foomft", QueryResource.AllSeries)]
        public void GivenFilterCondition_WithValidQueryString_ParseSucceeds(string queryString, QueryResource resourceType)
        {
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), resourceType), QueryTagService.CoreQueryTags);
        }

        [Fact]
        public void GivenExtendedQueryPrivateTag_WithUrl_ParseSucceeds()
        {
            DicomTag tag = new DicomTag(0x0405, 0x1001, "PrivateCreator1");
            string value = "Test";
            var queryString = $"{tag.GetPath()}={value}";
            QueryTag queryTag = new QueryTag(tag.BuildExtendedQueryTagStoreEntry(vr: DicomVRCode.CS, level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDateTag_WithUrl_ParseSucceeds()
        {
            var queryString = "Date=19510910-20200220";
            QueryTag queryTag = new QueryTag(DicomTag.Date.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDateTimeTag_WithUrl_ParseSucceeds()
        {
            var queryString = "DateTime=20200301195109.10-20200501195110.20";
            QueryTag queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Theory]
        [InlineData("19510910010203", "20200220020304")]
        public void GivenDateTime_WithValidRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(DicomTag.DateTime.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(string.Concat("DateTime=", minValue, "-", maxValue)), QueryResource.AllStudies), new[] { queryTag });
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.QueryTag.Tag == DicomTag.DateTime);
            Assert.True(cond.Minimum == DateTimeOffset.ParseExact(minValue, QueryParser.DateTimeTagValueFormats, null));
            Assert.True(cond.Maximum == DateTimeOffset.ParseExact(maxValue, QueryParser.DateTimeTagValueFormats, null));
        }

        [Fact]
        public void GivenExtendedQueryTimeTag_WithUrl_ParseSucceeds()
        {
            var queryString = "Time=195109.10-195110.20";
            QueryTag queryTag = new QueryTag(DicomTag.Time.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Theory]
        [InlineData("010203", "020304")]
        public void GivenStudyTime_WithValidRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(DicomTag.Time.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));

            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(string.Concat("Time=", minValue, "-", maxValue)), QueryResource.AllStudies), new[] { queryTag });
            var cond = queryExpression.FilterConditions.First() as LongRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.QueryTag.Tag == DicomTag.Time);
            Assert.True(cond.Minimum == DateTime.ParseExact(minValue, QueryParser.TimeTagValueFormats, null, System.Globalization.DateTimeStyles.NoCurrentDateDefault).Ticks);
            Assert.True(cond.Maximum == DateTime.ParseExact(maxValue, QueryParser.TimeTagValueFormats, null, System.Globalization.DateTimeStyles.NoCurrentDateDefault).Ticks);
        }

        [Fact]
        public void GivenExtendedQueryPersonNameTag_WithUrl_ParseSucceeds()
        {
            var queryString = "PatientBirthName=Joe&fuzzyMatching=true&limit=50";
            QueryTag queryTag = new QueryTag(DicomTag.PatientBirthName.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));

            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), new[] { queryTag });
            var fuzzyCondition = queryExpression.FilterConditions.First() as PersonNameFuzzyMatchCondition;
            Assert.NotNull(fuzzyCondition);
            Assert.Equal("Joe", fuzzyCondition.Value);
            Assert.Equal(queryTag, fuzzyCondition.QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryStringTag_WithUrl_ParseSucceeds()
        {
            var queryString = "ModelGroupUID=abc";
            QueryTag queryTag = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryStringTag_WithTagPathUrl_ParseSucceeds()
        {
            var queryString = "00687004=abc";
            QueryTag queryTag = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryLongTag_WithUrl_ParseSucceeds()
        {
            var queryString = "NumberOfAssessmentObservations=50";
            QueryTag queryTag = new QueryTag(DicomTag.NumberOfAssessmentObservations.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDoubleTag_WithUrl_ParseSucceeds()
        {
            var queryString = "FloatingPointValue=1.1";
            QueryTag queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), new[] { queryTag });
            Assert.Equal(queryTag, queryExpression.FilterConditions.First().QueryTag);
        }

        [Fact]
        public void GivenExtendedQueryDoubleTagWithInvalidValue_WithUrl_ParseFails()
        {
            var queryString = "FloatingPointValue=abc";
            QueryTag queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), new[] { queryTag }));
        }

        [Fact]
        public void GivenNonExistingExtendedQueryStringTag_WithUrl_ParseFails()
        {
            var queryString = "ModelGroupUID=abc";
            QueryTag queryTag = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), new[] { queryTag }));
        }

        [Fact]
        public void GivenCombinationOfExtendedQueryAndStandardTags_WithUrl_ParseSucceeds()
        {
            var queryString = "PatientName=Joe&FloatingPointValue=1.1&StudyDate=19510910-20200220&00687004=abc";
            QueryTag queryTag1 = new QueryTag(DicomTag.FloatingPointValue.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryTag queryTag2 = new QueryTag(DicomTag.ModelGroupUID.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Series));
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), QueryTagService.CoreQueryTags.Concat(new[] { queryTag1, queryTag2 }).ToList());
            Assert.Equal(4, queryExpression.FilterConditions.Count);
            Assert.Contains(queryTag1, queryExpression.FilterConditions.Select(x => x.QueryTag));
            Assert.Contains(queryTag2, queryExpression.FilterConditions.Select(x => x.QueryTag));
        }

        [Theory]
        [InlineData("PatientName=Joe&00100010=Rob")]
        [InlineData("00100010=Joe, Rob")]
        public void GivenFilterCondition_WithDuplicateQueryParam_Throws(string queryString)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("PatientName=  ")]
        [InlineData("PatientName=&fuzzyMatching=true")]
        [InlineData("StudyDescription=")]
        public void GivenFilterCondition_WithInvalidAttributeIdStringValue_Throws(string queryString)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("offset", "2.5")]
        [InlineData("offset", "-1")]
        public void GivenOffset_WithNotIntValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("offset", 25)]
        public void GivenOffset_WithIntValue_CheckOffset(string key, int value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.Offset == value);
        }

        [Theory]
        [InlineData("limit", "sdfsdf")]
        [InlineData("limit", "-2")]
        public void GivenLimit_WithInvalidValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("limit", "500000")]
        public void GivenLimit_WithMaxValueExceeded_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("limit", 50)]
        public void GivenLimit_WithValidValue_CheckLimit(string key, int value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.Limit == value);
        }

        [Theory]
        [InlineData("limit", 0)]
        public void GivenLimit_WithZero_ThrowsException(string key, int value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("00390061", "invalidtag")]
        [InlineData("unkownparam", "invalidtag")]
        public void GivenFilterCondition_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("fuzzymatching", "true")]
        public void GivenFuzzyMatch_WithValidValue_Check(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.True(queryExpression.FuzzyMatching);
        }

        [Theory]
        [InlineData("fuzzymatching", "notbool")]
        public void GivenFuzzyMatch_InvalidValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags));
        }

        [Theory]
        [InlineData("StudyDate", "19510910-20200220")]
        public void GivenStudyDate_WithValidRangeMatch_CheckCondition(string key, string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.QueryTag.Tag == DicomTag.StudyDate);
            Assert.True(cond.Minimum == DateTime.ParseExact(value.Split('-')[0], QueryParser.DateTagValueFormat, null));
            Assert.True(cond.Maximum == DateTime.ParseExact(value.Split('-')[1], QueryParser.DateTagValueFormat, null));
        }

        [Theory]
        [InlineData("StudyDate", "2020/02/28")]
        [InlineData("StudyDate", "20200230")]
        [InlineData("StudyDate", "20200228-20200230")]
        [InlineData("StudyDate", "20200110-20200109")]
        [InlineData("PerformedProcedureStepStartDate", "baddate")]
        public void GivenDateTag_WithInvalidDate_Throw(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllSeries), QueryTagService.CoreQueryTags));
        }

        [Fact]
        public void GivenStudyInstanceUID_WithUrl_CheckFilterCondition()
        {
            var testStudyInstanceUid = TestUidGenerator.Generate();
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(new Dictionary<string, string>()), QueryResource.AllSeries, testStudyInstanceUid), QueryTagService.CoreQueryTags);
            Assert.Equal(1, queryExpression.FilterConditions.Count);
            var cond = queryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(testStudyInstanceUid, cond.Value);
        }

        [Theory]
        [InlineData("PatientName=CoronaPatient&StudyDate=20200403&fuzzyMatching=true", QueryResource.AllStudies)]
        public void GivenPatientNameFilterCondition_WithFuzzyMatchingTrue_FuzzyMatchConditionAdded(string queryString, QueryResource resourceType)
        {
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), resourceType), QueryTagService.CoreQueryTags);

            Assert.Equal(2, queryExpression.FilterConditions.Count);

            var studyDateFilterCondition = queryExpression.FilterConditions.FirstOrDefault(c => c.QueryTag.Tag == DicomTag.StudyDate) as DateSingleValueMatchCondition;
            Assert.NotNull(studyDateFilterCondition);

            QueryFilterCondition patientNameCondition = queryExpression.FilterConditions.FirstOrDefault(c => c.QueryTag.Tag == DicomTag.PatientName);
            Assert.NotNull(patientNameCondition);

            var fuzzyCondition = patientNameCondition as PersonNameFuzzyMatchCondition;
            Assert.NotNull(fuzzyCondition);
            Assert.Equal("CoronaPatient", fuzzyCondition.Value);
        }

        private void VerifyIncludeFieldsForValidAttributeIds(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), QueryTagService.CoreQueryTags);
            Assert.False(queryExpression.HasFilters);
            Assert.False(queryExpression.IncludeFields.All);
            Assert.True(queryExpression.IncludeFields.DicomTags.Count == value.Split(',').Length);
        }

        private IEnumerable<KeyValuePair<string, StringValues>> GetQueryCollection(string key, string value)
        {
            return GetQueryCollection(new Dictionary<string, string>() { { key, value } });
        }

        private IEnumerable<KeyValuePair<string, StringValues>> GetQueryCollection(Dictionary<string, string> queryParams)
        {
            foreach (KeyValuePair<string, string> pair in queryParams)
            {
                yield return KeyValuePair.Create(pair.Key, new StringValues(pair.Value.Split(',')));
            }
        }

        private IEnumerable<KeyValuePair<string, StringValues>> GetQueryCollection(string queryString)
        {
            var parameters = queryString.Split('&');

            foreach (var param in parameters)
            {
                var keyValue = param.Split('=');

                yield return KeyValuePair.Create(keyValue[0], new StringValues(keyValue[1].Split(',')));
            }
        }

        private QueryResourceRequest CreateRequest(
            IEnumerable<KeyValuePair<string, StringValues>> queryParams,
            QueryResource resourceType,
            string studyInstanceUid = null,
            string seriesInstanceUid = null)
        {
            return new QueryResourceRequest(queryParams, resourceType, studyInstanceUid, seriesInstanceUid);
        }
    }
}
