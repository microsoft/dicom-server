// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages;
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

        [Fact]
        public void EmptyQueryString_IsEmptyProperty_True()
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(new QueryCollection(), ResourceType.Study);
            Assert.True(dicomQueryExpression.IsEmpty);
        }

        [Theory]
        [InlineData("includefield", "StudyDate")]
        [InlineData("includefield", "00100020")]
        [InlineData("includefield", "00100020,00100010")]
        [InlineData("includefield", "StudyDate, StudyTime")]
        public void IncludeField_AttributeId_Valid(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value), ResourceType.Study);
            Assert.False(dicomQueryExpression.IsEmpty);
            Assert.False(dicomQueryExpression.IncludeFields.All);
            Assert.True(dicomQueryExpression.IncludeFields.DicomTags.Count == value.Split(',').Count());
        }

        [Theory]
        [InlineData("includefield", "all")]
        public void IncludeField_AttributeId_ValidAll(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value), ResourceType.Study);
            Assert.True(dicomQueryExpression.IncludeFields.All);
        }

        [Theory]
        [InlineData("includefield", "something")]
        [InlineData("includefield", "00030033")]
        public void IncludeField_AttributeId_Invalid(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("00100010", "joe")]
        [InlineData("PatientName", "joe")]
        public void FilterCondition_AttributeId_Valid(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value), ResourceType.Study);
            Assert.False(dicomQueryExpression.IsEmpty);
            var singleValueCond = dicomQueryExpression.FilterConditions.First() as DicomQuerySingleValueMatchingCondition<string>;
            Assert.NotNull(singleValueCond);
            Assert.True(singleValueCond.DicomTag == DicomTag.PatientName);
            Assert.True(singleValueCond.Value == value);
        }

        [Theory]
        [InlineData("00080061", "CT")]
        public void FilterCondition_AttributeIdKeywordValid_NotSupported(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("Modality", "CT", ResourceType.Study)]
        [InlineData("SOPInstanceUID", "1.2.3.48898989", ResourceType.Series)]
        public void FilterCondition_AttributeIdKeywordValid_LevelNotSupported(string key, string value, ResourceType resourceType)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), resourceType));
        }

        [Theory]
        [InlineData("limit=25&offset=0&fuzzymatching=false&includefield=00081030,00080060&StudyDate=19510910-20200220", ResourceType.Study)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&limit=50", ResourceType.Study)]
        [InlineData("PatientName=Joe&fuzzyMatching=true&Modality=CT", ResourceType.Series)]
        public void QueryString_Valid(string queryString, ResourceType resourceType)
        {
            _queryParser.Parse(GetQueryCollection(queryString), resourceType);
        }

        [Theory]
        [InlineData("PatientName=Joe&00100010=Rob")]
        [InlineData("00100010=Joe, Rob")]
        public void DuplicateQueryParam_NotAllowed(string queryString)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(queryString), ResourceType.Study));
        }

        [Theory]
        [InlineData("offset", "2.5")]
        [InlineData("offset", "-1")]
        public void OffsetValue_NotInt(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("offset", 25)]
        public void OffsetValue_Valid(string key, int value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value.ToString()), ResourceType.Study);
            Assert.True(dicomQueryExpression.Offset == value);
        }

        [Theory]
        [InlineData("limit", "sdfsdf")]
        [InlineData("limit", "-2")]
        public void LimitValue_NotInt(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("limit", "500000")]
        public void LimitValue_MaxExceeded(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("limit", 50)]
        public void LimitValue_Valid(string key, int value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value.ToString()), ResourceType.Study);
            Assert.True(dicomQueryExpression.Limit == value);
        }

        [Theory]
        [InlineData("00390061", "invalidtag")]
        [InlineData("unkownparam", "invalidtag")]
        public void FilterCondition_AttributeId_Invalid(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("fuzzymatching", "true")]
        public void FuzzyMatch_ValidValue(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value), ResourceType.Study);
            Assert.True(dicomQueryExpression.FuzzyMatching);
        }

        [Theory]
        [InlineData("fuzzymatching", "notbool")]
        public void FuzzyMatch_InValidValue(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Study));
        }

        [Theory]
        [InlineData("StudyDate", "19510910-20200220")]
        public void StudyDate_ValidRangeMatch(string key, string value)
        {
            DicomQueryExpression dicomQueryExpression = _queryParser
                .Parse(GetQueryCollection(key, value), ResourceType.Study);
            var cond = dicomQueryExpression.FilterConditions.First() as DicomQueryRangeValueMatchingCondition<string>;
            Assert.NotNull(cond);
            Assert.True(cond.DicomTag == DicomTag.StudyDate);
            Assert.True(cond.Minimum == value.Split('-')[0]);
            Assert.True(cond.Maximum == value.Split('-')[1]);
        }

        [Theory]
        [InlineData("StudyDate", "2020/02/28")]
        [InlineData("StudyDate", "20200230")]
        [InlineData("StudyDate", "20200228-20200230")]
        [InlineData("PerformedProcedureStepStartDate", "baddate")]
        public void DateTagValue_InvalidDate(string key, string value)
        {
            Assert.Throws<DicomQueryParseException>(() => _queryParser
            .Parse(GetQueryCollection(key, value), ResourceType.Series));
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
    }
}
