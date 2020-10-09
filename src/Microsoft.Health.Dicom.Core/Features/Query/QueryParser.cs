// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Main parser class that implements the flow and registration of the parsers
    /// </summary>
    public partial class QueryParser : IQueryParser
    {
        private readonly ILogger<QueryParser> _logger;
        private const string IncludeFieldValueAll = "all";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        private QueryExpressionImp _parsedQuery = null;
        private readonly Dictionary<string, Action<KeyValuePair<string, StringValues>>> _paramParsers =
            new Dictionary<string, Action<KeyValuePair<string, StringValues>>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Func<DicomTag, string, QueryFilterCondition>> _valueParsers =
            new Dictionary<string, Func<DicomTag, string, QueryFilterCondition>>(StringComparer.OrdinalIgnoreCase);

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryParser(ILogger<QueryParser> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
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
                    if (_parsedQuery.FilterConditionAttributeIds.Contains(condition.AttributeId))
                    {
                        throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, queryParam.Key));
                    }

                    _parsedQuery.FilterConditionAttributeIds.Add(condition.AttributeId);
                    _parsedQuery.FilterConditions.Add(condition);
                    continue;
                }

                throw new QueryParseException(string.Format(DicomCoreResource.UnknownQueryParameter, queryParam.Key));
            }

            // add UIDs as filter conditions
            if (request.StudyInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new DicomAttributeId(DicomTag.StudyInstanceUID), request.StudyInstanceUid);
                _parsedQuery.FilterConditions.Add(condition);
            }

            if (request.SeriesInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new DicomAttributeId(DicomTag.SeriesInstanceUID), request.SeriesInstanceUid);
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

        private void PostProcessFilterConditions(QueryExpressionImp parsedQuery)
        {
            // fuzzy match condition modification
            if (parsedQuery.FuzzyMatch == true)
            {
                IEnumerable<QueryFilterCondition> potentialFuzzyConds = parsedQuery.FilterConditions
                                                                            .Where(c => QueryLimit.IsValidFuzzyMatchingQueryTag(c.AttributeId.Tag)).ToList();
                foreach (QueryFilterCondition cond in potentialFuzzyConds)
                {
                    var singleValueCondition = cond as StringSingleValueMatchCondition;

                    // Remove existing stringvalue match and add fuzzymatch condition
                    var personNameFuzzyMatchCondition = new PersonNameFuzzyMatchCondition(singleValueCondition.AttributeId, singleValueCondition.Value);
                    parsedQuery.FilterConditions.Remove(singleValueCondition);
                    parsedQuery.FilterConditions.Add(personNameFuzzyMatchCondition);
                }
            }
        }

        private bool ParseFilterCondition(KeyValuePair<string, StringValues> queryParameter, QueryResource resourceType, out QueryFilterCondition condition)
        {
            condition = null;
            var attributeIdText = queryParameter.Key.Trim();

            // parse tag
            if (!DicomAttributeId.TryParse(attributeIdText, out DicomAttributeId attributeId))
            {
                return false;
            }

            ValidateIfAttributeIdSupported(attributeId, attributeIdText, resourceType);

            // parse tag value
            if (!queryParameter.Value.Any() || queryParameter.Value.Count() > 1)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, attributeIdText));
            }

            var trimmedValue = queryParameter.Value.First().Trim();
            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.QueryEmptyAttributeValue, attributeIdText));
            }

            var tagTypeCode = attributeId.Tag.DictionaryEntry.ValueRepresentations.FirstOrDefault()?.Code;

            // ? can get tagTypeCode?  for private one?
            if (!attributeId.IsPrivate)
            {
                if (_valueParsers.TryGetValue(tagTypeCode, out Func<DicomTag, string, QueryFilterCondition> valueParser))
                {
                    condition = valueParser(attributeId.Tag, trimmedValue);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return condition != null;
        }

        private static void ValidateIfAttributeIdSupported(DicomAttributeId attributeId, string attributeIdText, QueryResource resourceType)
        {
            if (!attributeId.IsPrivate)
            {
                HashSet<DicomTag> supportedQueryTags = QueryLimit.QueryResourceTypeToTagsMapping[resourceType];

                if (!supportedQueryTags.Contains(attributeId.Tag))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedSearchParameter, attributeIdText));
                }
            }
        }

        private static DicomTag ParseDicomTagNumber(string s)
        {
            if (s.Length < 8)
            {
                return null;
            }

            if (!ushort.TryParse(s.Substring(0, 4), NumberStyles.HexNumber, null, out ushort group))
            {
                return null;
            }

            if (!ushort.TryParse(s.Substring(4, 4), NumberStyles.HexNumber, null, out ushort element))
            {
                return null;
            }

            var dicomTag = new DicomTag(group, element);
            DicomDictionaryEntry knownTag = DicomDictionary.Default[dicomTag];

            // Check if the tag is null or unknown.
            // Tag with odd group is considered as private.
            if (knownTag == null || (!dicomTag.IsPrivate && knownTag == DicomDictionary.UnknownTag))
            {
                return null;
            }

            return dicomTag;
        }

        private class QueryExpressionImp
        {
            public QueryExpressionImp()
            {
                IncludeFields = new HashSet<DicomAttributeId>();
                FilterConditions = new List<QueryFilterCondition>();
                FilterConditionAttributeIds = new HashSet<DicomAttributeId>();
            }

            public HashSet<DicomAttributeId> IncludeFields { get; set; }

            public bool FuzzyMatch { get; set; }

            public int Offset { get; set; }

            public int Limit { get; set; }

            public List<QueryFilterCondition> FilterConditions { get; set; }

            public bool AllValue { get; set; }

            public HashSet<DicomAttributeId> FilterConditionAttributeIds { get; set; }
        }
    }
}
