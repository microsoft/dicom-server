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
using Microsoft.Health.Dicom.Core.Messages;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Main parser class that implements the flow and registration of the parsers
    /// </summary>
    public partial class DicomQueryParser : IDicomQueryParser
    {
        private readonly ILogger<DicomQueryParser> _logger;
        private const string IncludeFieldValueAll = "all";
        private const string DateTagValueFormat = "yyyyMMdd";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        private QueryExpressionImp _parsedQuery = null;
        private readonly Dictionary<string, Action<KeyValuePair<string, StringValues>>> _paramParsers =
            new Dictionary<string, Action<KeyValuePair<string, StringValues>>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Func<DicomTag, string, DicomQueryFilterCondition>> _valueParsers =
            new Dictionary<string, Func<DicomTag, string, DicomQueryFilterCondition>>(StringComparer.OrdinalIgnoreCase);

        public DicomQueryParser(ILogger<DicomQueryParser> logger)
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

        public DicomQueryExpression Parse(QueryDicomResourceRequest request)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            _parsedQuery = new QueryExpressionImp();
            var filterConditionTags = new HashSet<DicomTag>();

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
                if (ParseFilterCondition(queryParam, request.QueryResourceType, out DicomQueryFilterCondition condition))
                {
                    if (filterConditionTags.Contains(condition.DicomTag))
                    {
                        throw new DicomQueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, queryParam.Key));
                    }

                    filterConditionTags.Add(condition.DicomTag);
                    _parsedQuery.FilterConditions.Add(condition);
                    continue;
                }

                throw new DicomQueryParseException(string.Format(DicomCoreResource.UnkownQueryParameter, queryParam.Key));
            }

            // add UIDs as filter conditions
            if (request.StudyInstanceUID != null)
            {
                var condition = new StringSingleValueMatchCondition(DicomTag.StudyInstanceUID, request.StudyInstanceUID);
                _parsedQuery.FilterConditions.Add(condition);
            }

            if (request.SeriesInstanceUID != null)
            {
                var condition = new StringSingleValueMatchCondition(DicomTag.SeriesInstanceUID, request.SeriesInstanceUID);
                _parsedQuery.FilterConditions.Add(condition);
            }

            return new DicomQueryExpression(
                request.QueryResourceType,
                new DicomQueryParameterIncludeField(_parsedQuery.AllValue, _parsedQuery.IncludeFields),
                _parsedQuery.FuzzyMatch,
                _parsedQuery.Limit,
                _parsedQuery.Offset,
                _parsedQuery.FilterConditions);
        }

        private bool ParseFilterCondition(KeyValuePair<string, StringValues> queryParameter, QueryResourceType resourceType, out DicomQueryFilterCondition condition)
        {
            condition = null;
            var attributeId = queryParameter.Key.Trim();

            // parse tag
            if (!TryParseDicomAttributeId(attributeId, out DicomTag dicomTag))
            {
                return false;
            }

            ValidateIfTagSupported(dicomTag, resourceType);

            // parse tag value
            if (!queryParameter.Value.Any() || queryParameter.Value.Count() > 1)
            {
                throw new DicomQueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, attributeId));
            }

            var trimmedValue = queryParameter.Value.First().Trim();
            var tagTypeCode = dicomTag.DictionaryEntry.ValueRepresentations.FirstOrDefault()?.Code;
            if (_valueParsers.TryGetValue(tagTypeCode, out Func<DicomTag, string, DicomQueryFilterCondition> valueParser))
            {
                condition = valueParser(dicomTag, trimmedValue);
            }

            return condition != null;
        }

        private bool TryParseDicomAttributeId(string attributeId, out DicomTag dicomTag)
        {
            dicomTag = null;

            // Try Keyword match, returns null if not found
            // fo-dicom github bug throwing nullreference https://github.com/fo-dicom/fo-dicom/issues/996
            try
            {
                dicomTag = DicomDictionary.Default[attributeId];
            }
            catch (NullReferenceException e)
            {
                _logger.LogDebug(e, $"DicomDictionary.Default[attributeId] threw exception for {attributeId}");
            }

            if (dicomTag == null)
            {
                dicomTag = ParseDicomTagNumber(attributeId);
            }

            return dicomTag != null;
        }

        private static void ValidateIfTagSupported(DicomTag dicomTag, QueryResourceType resourceType)
        {
            HashSet<DicomTag> supportedQueryTags = DicomQueryConditionLimit.QueryResourceTypeToTagsMapping[resourceType];

            if (!supportedQueryTags.Contains(dicomTag))
            {
                throw new DicomQueryParseException(DicomCoreResource.UnsupportedSearchParameter);
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
            if (knownTag == null || knownTag == DicomDictionary.UnknownTag || knownTag == DicomDictionary.PrivateCreatorTag)
            {
                return null;
            }

            return dicomTag;
        }

        private class QueryExpressionImp
        {
            public QueryExpressionImp()
            {
                IncludeFields = new HashSet<DicomTag>();
                FilterConditions = new List<DicomQueryFilterCondition>();
                FilterConditionTags = new HashSet<DicomTag>();
            }

            public HashSet<DicomTag> IncludeFields { get; set; }

            public bool FuzzyMatch { get; set; }

            public int Offset { get; set; }

            public int Limit { get; set; }

            public List<DicomQueryFilterCondition> FilterConditions { get; set; }

            public bool AllValue { get; set; }

            public HashSet<DicomTag> FilterConditionTags { get; set; }
        }
    }
}
