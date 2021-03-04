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

        private const string IncludeFieldValueAll = "all";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        private QueryExpressionImp _parsedQuery;
        private readonly Dictionary<string, Action<KeyValuePair<string, StringValues>>> _paramParsers =
            new Dictionary<string, Action<KeyValuePair<string, StringValues>>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<DicomVR, Func<DicomTag, string, DicomVR, QueryFilterCondition>> _valueParsers =
            new Dictionary<DicomVR, Func<DicomTag, string, DicomVR, QueryFilterCondition>>();

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryParser(IDicomTagParser dicomTagPathParser)
        {
            EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));
            _dicomTagPathParser = dicomTagPathParser;

            // register parameter parsers
            _paramParsers.Add("offset", ParseOffset);
            _paramParsers.Add("limit", ParseLimit);
            _paramParsers.Add("fuzzymatching", ParseFuzzyMatching);
            _paramParsers.Add("includefield", ParseIncludeField);

            // register value parsers
            _valueParsers.Add(DicomVR.DA, ParseDateTagValue);
            _valueParsers.Add(DicomVR.UI, ParseStringTagValue);
            _valueParsers.Add(DicomVR.LO, ParseStringTagValue);
            _valueParsers.Add(DicomVR.SH, ParseStringTagValue);
            _valueParsers.Add(DicomVR.PN, ParseStringTagValue);
            _valueParsers.Add(DicomVR.CS, ParseStringTagValue);

            _valueParsers.Add(DicomVR.DT, ParseDateTagValue);
            _valueParsers.Add(DicomVR.TM, ParseDateTagValue);

            _valueParsers.Add(DicomVR.AE, ParseStringTagValue);
            _valueParsers.Add(DicomVR.AS, ParseStringTagValue);
            _valueParsers.Add(DicomVR.DS, ParseStringTagValue);
            _valueParsers.Add(DicomVR.IS, ParseStringTagValue);

            _valueParsers.Add(DicomVR.AT, ParseLongTagValue);
            _valueParsers.Add(DicomVR.SL, ParseLongTagValue);
            _valueParsers.Add(DicomVR.SS, ParseLongTagValue);
            _valueParsers.Add(DicomVR.UL, ParseLongTagValue);
            _valueParsers.Add(DicomVR.US, ParseLongTagValue);

            _valueParsers.Add(DicomVR.FL, ParseDoubleTagValue);
            _valueParsers.Add(DicomVR.FD, ParseDoubleTagValue);
        }

        public QueryExpression Parse(QueryResourceRequest request, IDictionary<DicomTag, CustomTagFilterDetails> supportedCustomTags)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            var queriedCustomTags = new HashSet<CustomTagFilterDetails>();

            _parsedQuery = new QueryExpressionImp();

            foreach (KeyValuePair<string, StringValues> queryParam in request.RequestQuery)
            {
                var trimmedKey = queryParam.Key.Trim();

                // known keys
                if (_paramParsers.TryGetValue(trimmedKey, out Action<KeyValuePair<string, StringValues>> paramParser))
                {
                    paramParser(queryParam);
                    continue;
                }

                // filter conditions with attributeId as key
                if (ParseFilterCondition(queryParam, request.QueryResourceType, supportedCustomTags, out QueryFilterCondition condition))
                {
                    if (_parsedQuery.FilterConditionTags.Contains(condition.DicomTag))
                    {
                        throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, queryParam.Key));
                    }

                    _parsedQuery.FilterConditionTags.Add(condition.DicomTag);
                    _parsedQuery.FilterConditions.Add(condition);

                    if (condition.CustomTagFilterDetails != null)
                    {
                        queriedCustomTags.Add(condition.CustomTagFilterDetails);
                    }

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
                _parsedQuery.FilterConditions,
                queriedCustomTags);
        }

        private static void PostProcessFilterConditions(QueryExpressionImp parsedQuery)
        {
            // fuzzy match condition modification
            if (parsedQuery.FuzzyMatch == true)
            {
                for (int i = 0; i < parsedQuery.FilterConditions.Count; i++)
                {
                    QueryFilterCondition cond = parsedQuery.FilterConditions[i];
                    if (QueryLimit.IsValidFuzzyMatchingQueryTag(cond.DicomTag, cond.CustomTagFilterDetails?.VR))
                    {
                        var s = cond as StringSingleValueMatchCondition;
                        parsedQuery.FilterConditions[i] = new PersonNameFuzzyMatchCondition(s.DicomTag, s.Value);
                    }
                }
            }
        }

        private bool ParseFilterCondition(
            KeyValuePair<string, StringValues> queryParameter,
            QueryResource resourceType,
            IDictionary<DicomTag, CustomTagFilterDetails> supportedCustomTags,
            out QueryFilterCondition condition)
        {
            condition = null;
            var attributeId = queryParameter.Key.Trim();

            // parse tag
            if (!TryParseDicomAttributeId(attributeId, out DicomTag dicomTag))
            {
                return false;
            }

            CustomTagFilterDetails customTagFilterDetails;
            ValidateIfTagSupported(dicomTag, attributeId, resourceType, supportedCustomTags, out customTagFilterDetails);

            // parse tag value
            if (queryParameter.Value.Count != 1)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, attributeId));
            }

            var trimmedValue = queryParameter.Value.First().Trim();
            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.QueryEmptyAttributeValue, attributeId));
            }

            var tagTypeCode = customTagFilterDetails == null ? dicomTag.DictionaryEntry.ValueRepresentations.FirstOrDefault() : customTagFilterDetails.VR;
            if (_valueParsers.TryGetValue(tagTypeCode, out Func<DicomTag, string, DicomVR, QueryFilterCondition> valueParser))
            {
                condition = valueParser(dicomTag, trimmedValue, tagTypeCode);
            }

            condition.CustomTagFilterDetails = customTagFilterDetails;

            return condition != null;
        }

        private bool TryParseDicomAttributeId(string attributeId, out DicomTag dicomTag)
        {
            if (_dicomTagPathParser.TryParse(attributeId, out DicomTag[] result, supportMultiple: false))
            {
                dicomTag = result[0];
                return true;
            }

            dicomTag = null;
            return false;
        }

        private static void ValidateIfTagSupported(DicomTag dicomTag, string attributeId, QueryResource resourceType, IDictionary<DicomTag, CustomTagFilterDetails> supportedCustomTags, out CustomTagFilterDetails customTagFilterDetails)
        {
            customTagFilterDetails = null;
            HashSet<DicomTag> supportedQueryTags = QueryLimit.QueryResourceTypeToTagsMapping[resourceType];

            if (!supportedQueryTags.Contains(dicomTag))
            {
                if (supportedCustomTags != null && supportedCustomTags.TryGetValue(dicomTag, out customTagFilterDetails))
                {
                    return;
                }

                string genericResourceType = resourceType >= QueryResource.AllInstances ? "instance" : (resourceType >= QueryResource.AllSeries ? "series" : "study");

                throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedSearchParameter, attributeId, genericResourceType));
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
