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
        private readonly ICustomTagVRCodeRetriever _customTagVRCodeRetriever;
        private const string IncludeFieldValueAll = "all";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        private QueryExpressionImp _parsedQuery = null;
        private readonly Dictionary<string, Action<KeyValuePair<string, StringValues>>> _paramParsers =
            new Dictionary<string, Action<KeyValuePair<string, StringValues>>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Func<DicomAttributeId, string, QueryFilterCondition>> _valueParsers =
            new Dictionary<string, Func<DicomAttributeId, string, QueryFilterCondition>>(StringComparer.OrdinalIgnoreCase);

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryParser(ICustomTagVRCodeRetriever customTagVRCodeRetriever, ILogger<QueryParser> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(customTagVRCodeRetriever, nameof(customTagVRCodeRetriever));
            _logger = logger;
            _customTagVRCodeRetriever = customTagVRCodeRetriever;

            // register parameter parsers
            _paramParsers.Add("offset", ParseOffset);
            _paramParsers.Add("limit", ParseLimit);
            _paramParsers.Add("fuzzymatching", ParseFuzzyMatching);
            _paramParsers.Add("includefield", ParseIncludeField);

            // register value parsers
            // String
            _valueParsers.Add(DicomVRCode.AE, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.CS, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.LT, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.PN, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.SH, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.ST, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.UI, ParseStringTagValue);
            _valueParsers.Add(DicomVRCode.LO, ParseStringTagValue);

            // Integer
            // TODO: should care about range?
            _valueParsers.Add(DicomVRCode.AS, ParseIntTagValue);
            _valueParsers.Add(DicomVRCode.AT, ParseIntTagValue);
            _valueParsers.Add(DicomVRCode.IS, ParseIntTagValue);
            _valueParsers.Add(DicomVRCode.SL, ParseIntTagValue);
            _valueParsers.Add(DicomVRCode.SS, ParseIntTagValue);
            _valueParsers.Add(DicomVRCode.UL, ParseIntTagValue);
            _valueParsers.Add(DicomVRCode.US, ParseIntTagValue);

            // Decimal
            _valueParsers.Add(DicomVRCode.DS, ParseDecimalTagValue);
            _valueParsers.Add(DicomVRCode.FL, ParseDecimalTagValue);
            _valueParsers.Add(DicomVRCode.FD, ParseDecimalTagValue);

            // Date time
            // TODO: fully support date and time as per standard
            _valueParsers.Add(DicomVRCode.DT, ParseDateTagValue);
            _valueParsers.Add(DicomVRCode.TM, ParseDateTagValue);
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

            DicomVR tagTypeCode;
            if (attributeId.IsPrivate)
            {
                // TODO: make whole method async
                tagTypeCode = _customTagVRCodeRetriever.RetrieveAsync(attributeId, cancellationToken: default(System.Threading.CancellationToken)).Result;
            }
            else
            {
                tagTypeCode = attributeId.Tag.DictionaryEntry.ValueRepresentations.FirstOrDefault();
            }

            if (_valueParsers.TryGetValue(tagTypeCode?.Code, out Func<DicomAttributeId, string, QueryFilterCondition> valueParser))
            {
                condition = valueParser(attributeId, trimmedValue);
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
