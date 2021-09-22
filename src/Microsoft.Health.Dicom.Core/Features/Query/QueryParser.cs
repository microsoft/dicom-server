// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Main parser class that implements the flow and registration of the parsers
    /// </summary>
    public partial class QueryParser : IQueryParser
    {
        private readonly IDicomTagParser _dicomTagPathParser;

        private const string IncludeFieldValueAll = "all";
        private QueryExpressionImp _parsedQuery;

        private readonly Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>> _valueParsers =
            new Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>>();

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryParser(IDicomTagParser dicomTagPathParser)
        {
            EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));
            _dicomTagPathParser = dicomTagPathParser;

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

        public QueryExpression Parse(QueryParameters parameters, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(parameters, nameof(parameters));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            _parsedQuery = new QueryExpressionImp();
            queryTags = GetQualifiedQueryTags(queryTags, parameters.QueryResourceType);

            foreach (KeyValuePair<string, string> filter in parameters.Filters)
            {
                // filter conditions with attributeId as key
                if (!ParseFilterCondition(filter, queryTags, parameters.FuzzyMatching, out QueryFilterCondition condition))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.UnknownQueryParameter, filter.Key));
                }

                if (!_parsedQuery.FilterConditions.TryAdd(condition.QueryTag.Tag, condition))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.DuplicateAttribute, filter.Key));
                }
            }

            // add UIDs as filter conditions
            if (parameters.StudyInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new QueryTag(DicomTag.StudyInstanceUID), parameters.StudyInstanceUid);
                if (!_parsedQuery.FilterConditions.TryAdd(DicomTag.StudyInstanceUID, condition))
                {
                    throw new QueryParseException(DicomCoreResource.DisallowedStudyInstanceUIDAttribute);
                }
            }

            if (parameters.SeriesInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new QueryTag(DicomTag.SeriesInstanceUID), parameters.SeriesInstanceUid);
                if (!_parsedQuery.FilterConditions.TryAdd(DicomTag.SeriesInstanceUID, condition))
                {
                    throw new QueryParseException(DicomCoreResource.DisallowedSeriesInstanceUIDAttribute);
                }
            }

            return new QueryExpression(
                parameters.QueryResourceType,
                ParseIncludeFields(parameters.IncludeField),
                parameters.FuzzyMatching,
                parameters.Limit,
                parameters.Offset,
                _parsedQuery.FilterConditions.Values);
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

        private bool ParseFilterCondition(
            KeyValuePair<string, string> queryParameter,
            IEnumerable<QueryTag> queryTags,
            bool fuzzyMatching,
            out QueryFilterCondition condition)
        {
            condition = null;

            // parse tag
            if (!TryParseDicomAttributeId(queryParameter.Key, out DicomTag dicomTag))
            {
                return false;
            }

            QueryTag queryTag = GetSupportedQueryTag(dicomTag, queryParameter.Key, queryTags);

            if (string.IsNullOrWhiteSpace(queryParameter.Value))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.QueryEmptyAttributeValue, queryParameter.Key));
            }

            if (!_valueParsers.TryGetValue(queryTag.VR, out Func<QueryTag, string, QueryFilterCondition> valueParser))
            {
                return false;
            }

            condition = valueParser(queryTag, queryParameter.Value);
            if (fuzzyMatching && QueryLimit.IsValidFuzzyMatchingQueryTag(queryTag))
            {
                var s = condition as StringSingleValueMatchCondition;
                condition = new PersonNameFuzzyMatchCondition(s.QueryTag, s.Value);
            }

            return true;
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

        private QueryIncludeField ParseIncludeFields(IReadOnlyList<string> includeFields)
        {
            // Check if `all` is present as one of the values in IncludeField parameter.
            if (includeFields.Any(val => IncludeFieldValueAll.Equals(val, StringComparison.OrdinalIgnoreCase)))
            {
                return QueryIncludeField.AllFields;
            }

            var fields = new List<DicomTag>(includeFields.Count);
            foreach (string field in includeFields)
            {
                if (!TryParseDicomAttributeId(field, out DicomTag dicomTag))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.IncludeFieldUnknownAttribute, field));
                }

                fields.Add(dicomTag);
            }

            return new QueryIncludeField(fields);
        }

        private class QueryExpressionImp
        {
            public HashSet<DicomTag> IncludeFields { get; } = new HashSet<DicomTag>();

            public Dictionary<DicomTag, QueryFilterCondition> FilterConditions { get; } = new Dictionary<DicomTag, QueryFilterCondition>();

            public bool AllValue { get; set; }
        }
    }
}
