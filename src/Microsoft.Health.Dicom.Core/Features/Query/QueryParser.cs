// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        private QueryExpressionImp _parsedQuery;

        public const string DateTagValueFormat = "yyyyMMdd";

        public QueryExpression Parse(QueryResourceRequest request, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            _parsedQuery = new QueryExpressionImp();
            queryTags = GetQualifiedQueryTags(queryTags, request.QueryResourceType);

            foreach (KeyValuePair<string, StringValues> queryParam in request.RequestQuery)
            {
                var trimmedKey = queryParam.Key.Trim();

                // Parse known parameters
                if (QueryParamsParser.TryParse(queryParam, ref _parsedQuery))
                {
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

        private static bool ParseFilterCondition(
            KeyValuePair<string, StringValues> queryParameter,
            IEnumerable<QueryTag> queryTags,
            out QueryFilterCondition condition)
        {
            condition = null;
            var attributeId = queryParameter.Key.Trim();

            // parse tag
            if (!DicomTagParser.TryParse(attributeId, out DicomTag dicomTag))
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

            QueryTagValueParser.TryParseTagValue(queryTag, trimmedValue, out condition);

            condition.QueryTag = queryTag;

            return condition != null;
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
    }
}
