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
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Main parser class that implements the flow and registration of the parsers
    /// </summary>
    public partial class QueryParser : IQueryParser
    {
        private const string IncludeFieldValueAll = "all";
        private const StringComparison QueryParameterComparision = StringComparison.OrdinalIgnoreCase;
        private QueryExpressionImp _parsedQuery;
        private readonly Dictionary<string, Action<KeyValuePair<string, StringValues>>> _paramParsers =
            new Dictionary<string, Action<KeyValuePair<string, StringValues>>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>> _valueParsers =
            new Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>>();

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryParser()
        {
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

            _valueParsers.Add(DicomVR.AE, ParseStringTagValue);
            _valueParsers.Add(DicomVR.AS, ParseStringTagValue);
            _valueParsers.Add(DicomVR.DS, ParseStringTagValue);
            _valueParsers.Add(DicomVR.IS, ParseStringTagValue);

            _valueParsers.Add(DicomVR.SL, ParseLongTagValue);
            _valueParsers.Add(DicomVR.SS, ParseLongTagValue);
            _valueParsers.Add(DicomVR.UL, ParseLongTagValue);
            _valueParsers.Add(DicomVR.US, ParseLongTagValue);

            _valueParsers.Add(DicomVR.FL, ParseDoubleTagValue);
            _valueParsers.Add(DicomVR.FD, ParseDoubleTagValue);
        }

        public QueryExpression Parse(QueryResourceRequest request, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            _parsedQuery = new QueryExpressionImp();
            queryTags = GetQualifiedQueryTags(queryTags, request.QueryResourceType);

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
                if (ParseFilterCondition(queryParam, queryTags, out QueryFilterCondition condition))
                {
                    if (_parsedQuery.FilterConditions.Any(item => item.QueryTag.Tag == condition.QueryTag.Tag))
                    {
                        throw new QueryParseException(string.Format(DicomCoreResource.DuplicateQueryParam, queryParam.Key));
                    }

                    _parsedQuery.FilterConditions.Add(condition);

                    continue;
                }

                throw new QueryParseException(string.Format(DicomCoreResource.UnknownQueryParameter, queryParam.Key));
            }

            // add UIDs as filter conditions
            if (request.StudyInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new QueryTag(DicomTag.StudyInstanceUID), request.StudyInstanceUid);
                _parsedQuery.FilterConditions.Add(condition);
            }

            if (request.SeriesInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new QueryTag(DicomTag.SeriesInstanceUID), request.SeriesInstanceUid);
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

        private static IReadOnlyCollection<QueryTag> GetQualifiedQueryTags(IReadOnlyCollection<QueryTag> queryTags, QueryResource queryResource)
        {
            return queryTags.Where(tag =>
            {
                // extended query tag need to Ready to be used.
                if (tag.IsExtendedQueryTag && tag.ExtendedQueryTagStoreEntry.Status != ExtendedQueryTagStatus.Ready)
                {
                    return false;
                }

                // tag level should be qualified
                return QueryLimit.QueryResourceTypeToQueryLevelsMapping[queryResource].Contains(tag.Level);

            }).ToList();
        }

        private static void PostProcessFilterConditions(QueryExpressionImp parsedQuery)
        {
            // fuzzy match condition modification
            if (parsedQuery.FuzzyMatch == true)
            {
                for (int i = 0; i < parsedQuery.FilterConditions.Count; i++)
                {
                    QueryFilterCondition cond = parsedQuery.FilterConditions[i];
                    if (QueryLimit.IsValidFuzzyMatchingQueryTag(cond.QueryTag))
                    {
                        var s = cond as StringSingleValueMatchCondition;
                        parsedQuery.FilterConditions[i] = new PersonNameFuzzyMatchCondition(s.QueryTag, s.Value);
                    }
                }
            }
        }

        private bool ParseFilterCondition(
            KeyValuePair<string, StringValues> queryParameter,
            IEnumerable<QueryTag> queryTags,
            out QueryFilterCondition condition)
        {
            condition = null;
            var attributeId = queryParameter.Key.Trim();

            // parse tag
            if (!TryParseDicomAttributeId(attributeId, out DicomTag dicomTag))
            {
                return false;
            }

            QueryTag queryTag = GetSupportedQueryTag(dicomTag, attributeId, queryTags);

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

            if (_valueParsers.TryGetValue(queryTag.VR, out Func<QueryTag, string, QueryFilterCondition> valueParser))
            {
                condition = valueParser(queryTag, trimmedValue);
            }

            condition.QueryTag = queryTag;

            return condition != null;
        }

        private static bool TryParseDicomAttributeId(string attributeId, out DicomTag dicomTag)
        {
            return DicomTagParser.TryParse(attributeId, out dicomTag);
        }

        private static QueryTag GetSupportedQueryTag(DicomTag dicomTag, string attributeId, IEnumerable<QueryTag> queryTags)
        {
            QueryTag queryTag = queryTags.FirstOrDefault(item =>
            {
                // private tag from request doesn't have private creator, should do path comparison.
                if (dicomTag.IsPrivate)
                {
                    return item.Tag.GetPath() == dicomTag.GetPath();
                }

                return item.Tag == dicomTag;
            });

            if (queryTag == null)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedSearchParameter, attributeId));
            }

            return queryTag;
        }

        private class QueryExpressionImp
        {
            public QueryExpressionImp()
            {
                IncludeFields = new HashSet<DicomTag>();
                FilterConditions = new List<QueryFilterCondition>();
            }

            public HashSet<DicomTag> IncludeFields { get; set; }

            public bool FuzzyMatch { get; set; }

            public int Offset { get; set; }

            public int Limit { get; set; }

            public List<QueryFilterCondition> FilterConditions { get; set; }

            public bool AllValue { get; set; }

        }
    }
}
