// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class DicomQueryParserTests
    {
        private readonly DicomQueryParser _queryParser = null;

        public DicomQueryParserTests()
        {
            _queryParser = new DicomQueryParser(NullLogger<DicomQueryParser>.Instance);
        }

        [Theory]
        [InlineData("includefield", "StudyDate")]
        [InlineData("includefield", "00100020")]
        [InlineData("includefield", "00100020,00100010")]
        [InlineData("includefield", "StudyDate, StudyTime")]
        public void GivenIncludeField_WithValidAttributeId_CheckIncludeFields(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies));
            Assert.False(dicomQueryExpression.AnyFilters);
            Assert.False(dicomQueryExpression.IncludeFields.All);
            Assert.True(dicomQueryExpression.IncludeFields.DicomTags.Count == value.Split(',').Count());
        }

        [Theory]
        [InlineData("includefield", "all")]
        public void GivenIncludeField_WithValueAll_CheckAllValue(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies));
            Assert.True(dicomQueryExpression.IncludeFields.All);
        }

        [Theory]
        [InlineData("includefield", "something")]
        [InlineData("includefield", "00030033")]
        public void GivenIncludeField_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("00100010", "joe")]
        [InlineData("PatientName", "joe")]
        public void GivenFilterCondition_ValidTag_CheckProperties(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies));
            Assert.True(dicomQueryExpression.AnyFilters);
            var singleValueCond = dicomQueryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(singleValueCond);
            Assert.True(singleValueCond.DicomTag == DicomTag.PatientName);
            Assert.True(singleValueCond.Value == value);
        }

        [Theory]
        [InlineData("00080061", "CT")]
        public void GivenFilterCondition_WithNotSupportedTag_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("Modality", "CT", QueryResource.AllStudies)]
        [InlineData("SOPInstanceUID", "1.2.3.48898989", QueryResource.AllSeries)]
        [InlineData("PatientName", "Joe", QueryResource.StudySeries)]
        [InlineData("Modality", "CT", QueryResource.StudySeriesInstances)]
        public void GivenFilterCondition_WithKnownTagButNotSupportedAtLevel_Throws(string key, string value, QueryResource resourceType)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), resourceType)));
        }

        [Theory]
        [InlineData("limit=25&offset=0&fuzzymatching=false&includefield=00081030,00080060&StudyDate=19510910-20200220", QueryResource.AllStudies)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&limit=50", QueryResource.AllStudies)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&Modality=CT", QueryResource.AllSeries)]
        public void GivenFilterCondition_WithValidQueryString_ParseSucceeds(string queryString, QueryResource resourceType)
        {
            _queryParser.Parse(CreateRequest(GetQueryCollection(queryString), resourceType));
        }

        [Theory]
        [InlineData("PatientName=Joe&00100010=Rob")]
        [InlineData("00100010=Joe, Rob")]
        public void GivenFilterCondition_WithDuplicateQueryParam_Throws(string queryString)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(queryString), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("offset", "2.5")]
        [InlineData("offset", "-1")]
        public void GivenOffset_WithNotIntValue_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("offset", 25)]
        public void GivenOffset_WithIntValue_CheckOffset(string key, int value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies));
            Assert.True(dicomQueryExpression.Offset == value);
        }

        [Theory]
        [InlineData("limit", "sdfsdf")]
        [InlineData("limit", "-2")]
        public void GivenLimit_WithInvalidValue_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("limit", "500000")]
        public void GivenLimit_WithMaxValueExceeded_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("limit", 50)]
        public void GivenLimit_WithValidValue_CheckLimit(string key, int value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies));
            Assert.True(dicomQueryExpression.Limit == value);
        }

        [Theory]
        [InlineData("limit", 0)]
        public void GivenLimit_WithZero_CheckEvaluatedLimit(string key, int value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value.ToString()), QueryResource.AllStudies));
            Assert.True(dicomQueryExpression.EvaluatedLimit == DicomQueryLimit.DefaultQueryResultCount);
        }

        [Theory]
        [InlineData("00390061", "invalidtag")]
        [InlineData("unkownparam", "invalidtag")]
        public void GivenFilterCondition_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("fuzzymatching", "true")]
        public void GivenFuzzyMatch_WithValidValue_Check(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies));
            Assert.True(dicomQueryExpression.FuzzyMatching);
        }

        [Theory]
        [InlineData("fuzzymatching", "notbool")]
        public void GivenFuzzyMatch_InValidValue_Throws(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies)));
        }

        [Theory]
        [InlineData("StudyDate", "19510910-20200220")]
        public void GivenStudyDate_WithValidRangeMatch_CheckCondition(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllStudies));
            var cond = dicomQueryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.DicomTag == DicomTag.StudyDate);
            Assert.True(cond.Minimum == DateTime.ParseExact(value.Split('-')[0], DicomQueryParser.DateTagValueFormat, null));
            Assert.True(cond.Maximum == DateTime.ParseExact(value.Split('-')[1], DicomQueryParser.DateTagValueFormat, null));
        }

        [Theory]
        [InlineData("StudyDate", "2020/02/28")]
        [InlineData("StudyDate", "20200230")]
        [InlineData("StudyDate", "20200228-20200230")]
        [InlineData("PerformedProcedureStepStartDate", "baddate")]
        public void GivenDateTag_WithInvalidDate_Throw(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(CreateRequest(GetQueryCollection(key, value), QueryResource.AllSeries)));
        }

        [Fact]
        public void GivenStudyUID_WithUrl_CheckFilterCondition()
        {
            var testStudyUID = DicomUID.Generate();
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(CreateRequest(GetQueryCollection(new Dictionary<string, string>()), QueryResource.AllSeries, testStudyUID.UID));
            Assert.Equal(1, dicomQueryExpression.FilterConditions.Count);
            var cond = dicomQueryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(testStudyUID.UID, cond.Value);
        }

        private QueryCollection GetQueryCollection(string key, string value)
        {
            return GetQueryCollection(new Dictionary<string, string>() { { key, value } });
        }

        private QueryCollection GetQueryCollection(Dictionary<string, string> queryParams)
        {
            var pairs = new Dictionary<string, StringValues>();

            foreach (KeyValuePair<string, string> pair in queryParams)
            {
                pairs.Add(pair.Key, new StringValues(pair.Value.Split(',')));
            }

            return new QueryCollection(pairs);
        }

        private QueryCollection GetQueryCollection(string queryString)
        {
            var parameters = queryString.Split('&');
            var pairs = new Dictionary<string, StringValues>();
            foreach (var param in parameters)
            {
                var keyValue = param.Split('=');
                pairs.Add(keyValue[0], keyValue[1].Split(','));
            }

            return new QueryCollection(pairs);
        }

        private QueryDicomResourceRequest CreateRequest(
            QueryCollection queryParams,
            QueryResource resourceType,
            string studyInstanceUID = null,
            string seriesInstanceUID = null)
        {
            return new QueryDicomResourceRequest(queryParams, resourceType, studyInstanceUID, seriesInstanceUID);
        }
    }
}
