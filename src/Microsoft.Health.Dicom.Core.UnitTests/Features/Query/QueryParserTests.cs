// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class QueryParserTests
    {
        private readonly QueryParser _queryParser;

        public QueryParserTests()
        {
            _queryParser = new QueryParser(new DicomTagParser(), NullLogger<QueryParser>.Instance);
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
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null);
            Assert.True(queryExpression.IncludeFields.All);
        }

        [Theory]
        [InlineData("includefield", "something")]
        public void GivenIncludeField_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("includefield", "12050010")]
        [InlineData("includefield", "12051001")]
        private void GivenIncludeField_WithPrivateAttributeId_CheckIncludeFields(string key, string value)
        {
            VerifyIncludeFieldsForValidAttributeIds(key, value);
        }

        [Theory]
        [InlineData("includefield", "12345678")]
        [InlineData("includefield", "98765432")]
        private void GivenIncludeField_WithUnknownAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("00100010", "joe")]
        [InlineData("PatientName", "joe")]
        public void GivenFilterCondition_ValidTag_CheckProperties(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null);
            Assert.True(queryExpression.HasFilters);
            var singleValueCond = queryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(singleValueCond);
            Assert.True(singleValueCond.DicomTag == DicomTag.PatientName);
            Assert.True(singleValueCond.Value == value);
        }

        [Theory]
        [InlineData("ReferringPhysicianName", "dr^joe")]
        public void GivenFilterCondition_ValidReferringPhysicianNameTag_CheckProperties(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null);
            Assert.True(queryExpression.HasFilters);
            var singleValueCond = queryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(singleValueCond);
            Assert.Equal(DicomTag.ReferringPhysicianName, singleValueCond.DicomTag);
            Assert.Equal(value, singleValueCond.Value);
        }

        [Theory]
        [InlineData("00080061", "CT")]
        public void GivenFilterCondition_WithNotSupportedTag_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("Modality", "CT", QueryResource.AllStudies)]
        [InlineData("SOPInstanceUID", "1.2.3.48898989", QueryResource.AllSeries)]
        [InlineData("PatientName", "Joe", QueryResource.StudySeries)]
        [InlineData("Modality", "CT", QueryResource.StudySeriesInstances)]
        public void GivenFilterCondition_WithKnownTagButNotSupportedAtLevel_Throws(string key, string value, QueryResource resourceType)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), resourceType), null));
        }

        [Theory]
        [InlineData("limit=25&offset=0&fuzzymatching=false&includefield=00081030,00080060&StudyDate=19510910-20200220", QueryResource.AllStudies)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&limit=50", QueryResource.AllStudies)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&Modality=CT", QueryResource.AllSeries)]
        public void GivenFilterCondition_WithValidQueryString_ParseSucceeds(string queryString, QueryResource resourceType)
        {
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), resourceType), null);
        }

        [Fact]
        public void GivenCustomDateTag_WithUrl_ParseSucceeds()
        {
            var queryString = "Date=19510910-20200220";
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Instance, DicomTag.Date);
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> filterDetailsByResource = new Dictionary<QueryResource, HashSet<CustomTagFilterDetails>>();
            filterDetailsByResource.Add(QueryResource.AllStudies, new HashSet<CustomTagFilterDetails>() { filterDetails });
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), filterDetailsByResource);
            Assert.Contains(filterDetails, queryExpression.QueriedCustomTagFilterDetails);
            Assert.Equal(filterDetails, queryExpression.FilterConditions.First().CustomTagFilterDetails);
        }

        [Fact]
        public void GivenCustomPersonNameTag_WithUrl_ParseSucceeds()
        {
            var queryString = "PatientBirthName=Joe&fuzzyMatching=true&limit=50";
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.PatientBirthName);
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> filterDetailsByResource = new Dictionary<QueryResource, HashSet<CustomTagFilterDetails>>();
            filterDetailsByResource.Add(QueryResource.AllSeries, new HashSet<CustomTagFilterDetails>() { filterDetails });
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), filterDetailsByResource);
            Assert.Contains(filterDetails, queryExpression.QueriedCustomTagFilterDetails);
        }

        [Fact]
        public void GivenCustomStringTag_WithUrl_ParseSucceeds()
        {
            var queryString = "ModelGroupUID=abc";
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.ModelGroupUID);
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> filterDetailsByResource = new Dictionary<QueryResource, HashSet<CustomTagFilterDetails>>();
            filterDetailsByResource.Add(QueryResource.AllSeries, new HashSet<CustomTagFilterDetails>() { filterDetails });
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), filterDetailsByResource);
            Assert.Contains(filterDetails, queryExpression.QueriedCustomTagFilterDetails);
            Assert.Equal(filterDetails, queryExpression.FilterConditions.First().CustomTagFilterDetails);
        }

        [Fact]
        public void GivenCustomLongTag_WithUrl_ParseSucceeds()
        {
            var queryString = "NumberOfAssessmentObservations=50";
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.NumberOfAssessmentObservations);
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> filterDetailsByResource = new Dictionary<QueryResource, HashSet<CustomTagFilterDetails>>();
            filterDetailsByResource.Add(QueryResource.AllSeries, new HashSet<CustomTagFilterDetails>() { filterDetails });
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), filterDetailsByResource);
            Assert.Contains(filterDetails, queryExpression.QueriedCustomTagFilterDetails);
            Assert.Equal(filterDetails, queryExpression.FilterConditions.First().CustomTagFilterDetails);
        }

        [Fact]
        public void GivenCustomDoubleTag_WithUrl_ParseSucceeds()
        {
            var queryString = "FloatingPointValue=1.1";
            var filterDetails = new CustomTagFilterDetails(1, CustomTagLevel.Series, DicomTag.FloatingPointValue);
            EnsureArg.IsNotNull(queryString, nameof(queryString));
            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> filterDetailsByResource = new Dictionary<QueryResource, HashSet<CustomTagFilterDetails>>();
            filterDetailsByResource.Add(QueryResource.AllSeries, new HashSet<CustomTagFilterDetails>() { filterDetails });
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllSeries), filterDetailsByResource);
            Assert.Contains(filterDetails, queryExpression.QueriedCustomTagFilterDetails);
            Assert.Equal(filterDetails, queryExpression.FilterConditions.First().CustomTagFilterDetails);
        }

        [Theory]
        [InlineData("PatientName=Joe&00100010=Rob")]
        [InlineData("00100010=Joe, Rob")]
        public void GivenFilterCondition_WithDuplicateQueryParam_Throws(string queryString)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("PatientName=  ")]
        [InlineData("PatientName=&fuzzyMatching=true")]
        [InlineData("StudyDescription=")]
        public void GivenFilterCondition_WithInvalidAttributeIdStringValue_Throws(string queryString)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("offset", "2.5")]
        [InlineData("offset", "-1")]
        public void GivenOffset_WithNotIntValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("offset", 25)]
        public void GivenOffset_WithIntValue_CheckOffset(string key, int value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies), null);
            Assert.True(queryExpression.Offset == value);
        }

        [Theory]
        [InlineData("limit", "sdfsdf")]
        [InlineData("limit", "-2")]
        public void GivenLimit_WithInvalidValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("limit", "500000")]
        public void GivenLimit_WithMaxValueExceeded_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("limit", 50)]
        public void GivenLimit_WithValidValue_CheckLimit(string key, int value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies), null);
            Assert.True(queryExpression.Limit == value);
        }

        [Theory]
        [InlineData("limit", 0)]
        public void GivenLimit_WithZero_ThrowsException(string key, int value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("00390061", "invalidtag")]
        [InlineData("unkownparam", "invalidtag")]
        public void GivenFilterCondition_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("fuzzymatching", "true")]
        public void GivenFuzzyMatch_WithValidValue_Check(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null);
            Assert.True(queryExpression.FuzzyMatching);
        }

        [Theory]
        [InlineData("fuzzymatching", "notbool")]
        public void GivenFuzzyMatch_InvalidValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null));
        }

        [Theory]
        [InlineData("StudyDate", "19510910-20200220")]
        public void GivenStudyDate_WithValidRangeMatch_CheckCondition(string key, string value)
        {
            EnsureArg.IsNotNull(value, nameof(value));
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null);
            var cond = queryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.DicomTag == DicomTag.StudyDate);
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
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllSeries), null));
        }

        [Fact]
        public void GivenStudyInstanceUID_WithUrl_CheckFilterCondition()
        {
            var testStudyInstanceUid = TestUidGenerator.Generate();
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(new Dictionary<string, string>()), QueryResource.AllSeries, testStudyInstanceUid), null);
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
            QueryExpression queryExpression = _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), resourceType), null);

            Assert.Equal(2, queryExpression.FilterConditions.Count);

            var studyDateFilterCondition = queryExpression.FilterConditions.FirstOrDefault(c => c.DicomTag == DicomTag.StudyDate) as DateSingleValueMatchCondition;
            Assert.NotNull(studyDateFilterCondition);

            var patientNameCondition = queryExpression.FilterConditions.FirstOrDefault(c => c.DicomTag == DicomTag.PatientName);
            Assert.NotNull(patientNameCondition);

            var fuzzyCondition = patientNameCondition as PersonNameFuzzyMatchCondition;
            Assert.NotNull(fuzzyCondition);
            Assert.Equal("CoronaPatient", fuzzyCondition.Value);
        }

        private void VerifyIncludeFieldsForValidAttributeIds(string key, string value)
        {
            QueryExpression queryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies), null);
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
