// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class WorkitemQueryParserTests
    {
        private readonly WorkitemQueryParser _queryParser;

        private readonly IReadOnlyList<QueryTag> _queryTags;

        public WorkitemQueryParserTests()
        {
            _queryParser = new WorkitemQueryParser(new DicomTagParser());

            _queryTags = WorkitemQueryResponseBuilder.RequiredReturnTags
                .Select(x =>
                    {
                        var entry = new WorkitemQueryTagStoreEntry(0, x.GetPath(), x.GetDefaultVR().Code);
                        entry.PathTags = Array.AsReadOnly(new DicomTag[] { x });
                        return new QueryTag(entry);
                    })
                .ToList();
        }

        [Fact]
        public void GivenParameters_WhenParsing_ThenForwardValues()
        {
            var parameters = new BaseQueryParameters
            {
                Filters = new Dictionary<string, string>(),
                FuzzyMatching = true,
                IncludeField = Array.Empty<string>(),
                Limit = 12,
                Offset = 700,
            };

            BaseQueryExpression actual = _queryParser.Parse(parameters, Array.Empty<QueryTag>());
            Assert.Equal(parameters.FuzzyMatching, actual.FuzzyMatching);
            Assert.Equal(parameters.Limit, actual.Limit);
            Assert.Equal(parameters.Offset, actual.Offset);
        }

        [Fact]
        public void GivenIncludeField_WithValueAll_CheckAllValue()
        {
            BaseQueryExpression BaseQueryExpression = _queryParser.Parse(
                CreateParameters(new Dictionary<string, string>(), includeField: new string[] { "all" }),
                _queryTags);
            Assert.True(BaseQueryExpression.IncludeFields.All);
        }

        [Fact]
        public void GivenIncludeField_WithInvalidAttributeId_Throws()
        {
            Assert.Throws<QueryParseException>(() => _queryParser.Parse(
                CreateParameters(new Dictionary<string, string>(), includeField: new string[] { "something" }),
                _queryTags));
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
                .Parse(CreateParameters(GetSingleton(key, value)), _queryTags));
        }

        [Theory]
        [InlineData("00100010", "joe")]
        [InlineData("PatientName", "joe")]
        public void GivenFilterCondition_ValidTag_CheckProperties(string key, string value)
        {
            BaseQueryExpression BaseQueryExpression = _queryParser
                .Parse(CreateParameters(GetSingleton(key, value)), _queryTags);
            Assert.True(BaseQueryExpression.HasFilters);
            var singleValueCond = BaseQueryExpression.FilterConditions.First() as StringSingleValueMatchCondition;
            Assert.NotNull(singleValueCond);
            Assert.True(singleValueCond.QueryTag.Tag == DicomTag.PatientName);
            Assert.True(singleValueCond.Value == value);
        }

        [Theory]
        [InlineData("19510910010203", "20200220020304")]
        public void GivenDateTime_WithValidRangeMatch_CheckCondition(string minValue, string maxValue)
        {
            EnsureArg.IsNotNull(minValue, nameof(minValue));
            EnsureArg.IsNotNull(maxValue, nameof(maxValue));
            QueryTag queryTag = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404005", 1, "DT"));

            BaseQueryExpression BaseQueryExpression = _queryParser.Parse(CreateParameters(GetSingleton("00404005", string.Concat(minValue, "-", maxValue))), new[] { queryTag });
            var cond = BaseQueryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.True(cond.QueryTag.Tag == DicomTag.ScheduledProcedureStepStartDateTime);
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
            QueryTag queryTag = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404005", 1, "DT"));

            BaseQueryExpression BaseQueryExpression = _queryParser.Parse(CreateParameters(GetSingleton("00404005", string.Concat(minValue, "-", maxValue))), new[] { queryTag });
            var cond = BaseQueryExpression.FilterConditions.First() as DateRangeValueMatchCondition;
            Assert.NotNull(cond);
            Assert.Equal(DicomTag.ScheduledProcedureStepStartDateTime, cond.QueryTag.Tag);

            DateTime expectedMin = string.IsNullOrEmpty(minValue) ? DateTime.MinValue : DateTime.ParseExact(minValue, QueryParser.DateTimeTagValueFormats, null);
            DateTime expectedMax = string.IsNullOrEmpty(maxValue) ? DateTime.MaxValue : DateTime.ParseExact(maxValue, QueryParser.DateTimeTagValueFormats, null);
            Assert.Equal(expectedMin, cond.Minimum);
            Assert.Equal(expectedMax, cond.Maximum);
        }

        [Fact]
        public void GivenDateTime_WithEmptyMinAndMaxInRangeMatch_Throw()
        {
            QueryTag queryTag = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404005", 1, "DT"));
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton("DateTime", "-")), new[] { queryTag }));
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
                    }),
                _queryTags));
        }

        [Theory]
        [InlineData("PatientName", "  ")]
        [InlineData("PatientName", "")]
        public void GivenFilterCondition_WithInvalidAttributeIdStringValue_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value)), _queryTags));
        }

        [Theory]
        [InlineData("00390061", "invalidtag")]
        [InlineData("unkownparam", "invalidtag")]
        public void GivenFilterCondition_WithInvalidAttributeId_Throws(string key, string value)
        {
            Assert.Throws<QueryParseException>(() => _queryParser
                .Parse(CreateParameters(GetSingleton(key, value)), _queryTags));
        }

        [Theory]
        [InlineData("0040A370.00080050", "Foo")]
        public void GivenWorkitemQueryTag_WithValidValue_ThenReturnsSuccessfully(string key, string value)
        {
            EnsureArg.IsNotNull(key, nameof(key));
            EnsureArg.IsNotNull(value, nameof(value));
            var item = new DicomSequence(
                    DicomTag.ReferencedRequestSequence,
                        new DicomDataset[] {
                            new DicomDataset(
                                new DicomShortString(
                                DicomTag.AccessionNumber, "Foo"),
                                new DicomShortString(
                                DicomTag.RequestedProcedureID, "Bar"))});
            QueryTag[] tags = new QueryTag[]
            {
              new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("0040A370.00080050", 1, item.ValueRepresentation.Code))
            };

            var expectedQueryTag = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00080050", 1, DicomTag.AccessionNumber.GetDefaultVR().Code));
            BaseQueryExpression BaseQueryExpression = _queryParser
                 .Parse(CreateParameters(GetSingleton(key, value)), tags);
            Assert.Equal(expectedQueryTag.Tag, BaseQueryExpression.FilterConditions.First().QueryTag.Tag);
        }

        [Fact]
        public void GivenTwoWorkitemQueryTags_WithSameLastLevelKey_ThenReturnsSuccessfully()
        {
            var item1 = new DicomSequence(
                    DicomTag.ScheduledStationClassCodeSequence,
                        new DicomDataset[] {
                            new DicomDataset(
                                new DicomShortString(
                                DicomTag.CodeValue, "Foo"))});

            var item2 = new DicomSequence(
                    DicomTag.ScheduledStationNameCodeSequence,
                        new DicomDataset[] {
                            new DicomDataset(
                                new DicomShortString(
                                DicomTag.CodeValue, "Bar"))});

            QueryTag[] tags = new QueryTag[]
            {
              new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404025.00080100", 1, item1.ValueRepresentation.Code)),
              new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00404026.00080100", 2, item2.ValueRepresentation.Code))
            };

            var expectedQueryTag = new QueryTag(Tests.Common.Extensions.DicomTagExtensions.BuildWorkitemQueryTagStoreEntry("00080100", 1, DicomTag.AccessionNumber.GetDefaultVR().Code));
            var filterConditions = new Dictionary<string, string>();
            filterConditions.Add("00404025.00080100", "Foo");
            filterConditions.Add("00404026.00080100", "Bar");

            BaseQueryExpression BaseQueryExpression = _queryParser
                 .Parse(CreateParameters(filterConditions), tags);

            Assert.Equal(2, BaseQueryExpression.FilterConditions.Count);
            Assert.Equal(expectedQueryTag.Tag, BaseQueryExpression.FilterConditions.First().QueryTag.Tag);
            Assert.Equal(expectedQueryTag.Tag, BaseQueryExpression.FilterConditions.Last().QueryTag.Tag);
        }

        private void VerifyIncludeFieldsForValidAttributeIds(params string[] values)
        {
            BaseQueryExpression BaseQueryExpression = _queryParser.Parse(
                CreateParameters(new Dictionary<string, string>(), includeField: values),
                _queryTags);

            Assert.False(BaseQueryExpression.HasFilters);
            Assert.False(BaseQueryExpression.IncludeFields.All);
            Assert.Equal(values.Length, BaseQueryExpression.IncludeFields.DicomTags.Count);
        }

        private Dictionary<string, string> GetSingleton(string key, string value)
            => new Dictionary<string, string> { { key, value } };

        private BaseQueryParameters CreateParameters(
            Dictionary<string, string> filters,
            bool fuzzyMatching = false,
            string[] includeField = null)
        {
            return new BaseQueryParameters
            {
                Filters = filters,
                FuzzyMatching = fuzzyMatching,
                IncludeField = includeField ?? Array.Empty<string>(),
            };
        }
    }
}
