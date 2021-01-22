// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Main parser class that implements the flow and registration of the parsers
    /// </summary>
    public partial class QueryParser : IQueryParser
    {
        private readonly IDicomTagParser _dicomTagPathParser;
        private readonly ILogger<QueryParser> _logger;

        private const string IncludeFieldValueAll = "all";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        private QueryExpressionImp _parsedQuery;
        private readonly Dictionary<string, Action<KeyValuePair<string, StringValues>>> _paramParsers =
            new Dictionary<string, Action<KeyValuePair<string, StringValues>>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Func<DicomTag, string, QueryFilterCondition>> _valueParsers =
            new Dictionary<string, Func<DicomTag, string, QueryFilterCondition>>(StringComparer.OrdinalIgnoreCase);

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryParser(IDicomTagParser dicomTagPathParser, ILogger<QueryParser> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));
            _dicomTagPathParser = dicomTagPathParser;
            _logger = logger;

            // register parameter parsers
            _paramParsers.Add("offset", ParseOffset);
            _paramParsers.Add("limit", ParseLimit);
            _paramParsers.Add("fuzzymatching", ParseFuzzyMatching);
            _paramParsers.Add("includefield", ParseIncludeField);

            // register value parsers
            _valueParsers.Add(DicomVRCode.DA, ParseDateTagValue);
            _valueParsers.Add(DicomVRCode.UI, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.LO, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.SH, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.PN, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.CS, ParseStringTagValue);
        }

        public QueryExpression Parse(QueryResourceRequest request)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            _parsedQuery = new QueryExpressionImp();

            foreach (var queryParam in request.RequestQuery)
            {
                var trimmedKey = queryParam.Key.Trim();

                // known keys
                if (_paramParsers.TryGetValue(trimmedKey, out Action<KeyValuePair<string, StringValues>> paramParser))
                {
                    paramParser(queryParam);
                    continue;
                }

                // filter conditions with attributeId as key
                if (ParseFilterCondition(queryParam, request.QueryResourceType, out QueryFilterCondition condition))
                {
                    if (_parsedQuery.FilterConditionTags.Contains(condition.DicomTag))
                    {
                        throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, queryParam.Key));
                    }

                    _parsedQuery.FilterConditionTags.Add(condition.DicomTag);
                    _parsedQuery.FilterConditions.Add(condition);
                    continue;
                }

                throw new QueryParseException(string.Format(DicomCoreResource.UnknownQueryParameter, queryParam.Key));
            }

            // add UIDs as filter conditions
            if (request.StudyInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, request.StudyInstanceUid);
                _parsedQuery.FilterConditions.Add(condition);
            }

            if (request.SeriesInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(DicomTag.SeriesInstanceUID, request.SeriesInstanceUid);
                _parsedQuery.FilterConditions.Add(condition);
            }

            PostProcessFilterConditions(_parsedQuery);

            return new QueryExpression(
                request.QueryResourceType,
                new QueryIncludeField(_parsedQuery.AllValue, _parsedQuery.IncludeFields),
                _parsedQuery.FuzzyMatch,
                _parsedQuery.Limit,
                _parsedQuery.Offset,
                _parsedQuery.FilterConditions);
        }

        private static void PostProcessFilterConditions(QueryExpressionImp parsedQuery)
        {
            // fuzzy match condition modification
            if (parsedQuery.FuzzyMatch == true)
            {
                IEnumerable<QueryFilterCondition> potentialFuzzyConds = parsedQuery.FilterConditions
                                                                            .Where(c => QueryLimit.IsValidFuzzyMatchingQueryTag(c.DicomTag)).ToList();
                foreach (QueryFilterCondition cond in potentialFuzzyConds)
                {
                    var singleValueCondition = cond as StringSingleValueMatchCondition;

                    // Remove existing stringvalue match and add fuzzymatch condition
                    var personNameFuzzyMatchCondition = new PersonNameFuzzyMatchCondition(singleValueCondition.DicomTag, singleValueCondition.Value);
                    parsedQuery.FilterConditions.Remove(singleValueCondition);
                    parsedQuery.FilterConditions.Add(personNameFuzzyMatchCondition);
                }
            }
        }

        private bool ParseFilterCondition(KeyValuePair<string, StringValues> queryParameter, QueryResource resourceType, out QueryFilterCondition condition)
        {
            condition = null;
            var attributeId = queryParameter.Key.Trim();

            // parse tag
            if (!TryParseDicomAttributeId(attributeId, out DicomTag dicomTag))
            {
                return false;
            }

            ValidateIfTagSupported(dicomTag, attributeId, resourceType);

            // parse tag value
            if (!queryParameter.Value.Any() || queryParameter.Value.Count > 1)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, attributeId));
            }

            var trimmedValue = queryParameter.Value.First().Trim();
            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.QueryEmptyAttributeValue, attributeId));
            }

            var tagTypeCode = dicomTag.DictionaryEntry.ValueRepresentations.FirstOrDefault()?.Code;
            if (_valueParsers.TryGetValue(tagTypeCode, out Func<DicomTag, string, QueryFilterCondition> valueParser))
            {
                condition = valueParser(dicomTag, trimmedValue);
            }

            return condition != null;
        }

        private bool TryParseDicomAttributeId(string attributeId, out DicomTag dicomTag)
        {
            dicomTag = null;
            DicomTag[] result;
            bool succeed = _dicomTagPathParser.TryParse(attributeId, out result, supportMultiple: false);
            if (succeed)
            {
                dicomTag = result[0];
            }

            return succeed;
        }

        private static void ValidateIfTagSupported(DicomTag dicomTag, string attributeId, QueryResource resourceType)
        {
            HashSet<DicomTag> supportedQueryTags = QueryLimit.QueryResourceTypeToTagsMapping[resourceType];

            if (!supportedQueryTags.Contains(dicomTag))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedSearchParameter, attributeId));
            }
        }

        private class QueryExpressionImp
        {
            public QueryExpressionImp()
            {
                IncludeFields = new HashSet<DicomTag>();
                FilterConditions = new List<QueryFilterCondition>();
                FilterConditionTags = new HashSet<DicomTag>();
            }

            public HashSet<DicomTag> IncludeFields { get; set; }

            public bool FuzzyMatch { get; set; }

            public int Offset { get; set; }

            public int Limit { get; set; }

            public List<QueryFilterCondition> FilterConditions { get; set; }

            public bool AllValue { get; set; }

            public HashSet<DicomTag> FilterConditionTags { get; set; }
        }
    }
}
