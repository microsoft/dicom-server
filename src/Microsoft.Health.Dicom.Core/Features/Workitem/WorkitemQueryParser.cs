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
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Main parser class that converts uri query parameters to sql ready query expresions for workitem search request
    /// </summary>
    public class WorkitemQueryParser : BaseQueryParser<BaseQueryExpression, BaseQueryParameters>
    {
        private readonly IDicomTagParser _dicomTagPathParser;

        public WorkitemQueryParser(IDicomTagParser dicomTagPathParser)
            => _dicomTagPathParser = EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));

        public override BaseQueryExpression Parse(BaseQueryParameters parameters, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(parameters, nameof(parameters));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            var filterConditions = new Dictionary<DicomTag, QueryFilterCondition>();
            foreach (KeyValuePair<string, string> filter in parameters.Filters)
            {
                // filter conditions with attributeId as key
                if (!ParseFilterCondition(filter, queryTags, parameters.FuzzyMatching, out QueryFilterCondition condition))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedSearchParameter, filter.Key));
                }

                if (!filterConditions.TryAdd(condition.QueryTag.Tag, condition))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.DuplicateAttribute, filter.Key));
                }
            }

            return new BaseQueryExpression(
                ParseIncludeFields(parameters.IncludeField),
                parameters.FuzzyMatching,
                parameters.Limit,
                parameters.Offset,
                filterConditions.Values);
        }

        private bool ParseFilterCondition(
            KeyValuePair<string, string> queryParameter,
            IEnumerable<QueryTag> queryTags,
            bool fuzzyMatching,
            out QueryFilterCondition condition)
        {
            condition = null;

            // parse tag
            if (!TryParseDicomAttributeId(queryParameter.Key, out DicomTag[] dicomTags))
            {
                return false;
            }

            QueryTag queryTag = GetMatchingQueryTag(dicomTags, queryParameter.Key, queryTags);

            if (string.IsNullOrWhiteSpace(queryParameter.Value))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.QueryEmptyAttributeValue, queryParameter.Key));
            }

            if (!ValueParsers.TryGetValue(queryTag.VR, out Func<QueryTag, string, QueryFilterCondition> valueParser))
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

        private bool TryParseDicomAttributeId(string attributeId, out DicomTag[] dicomTags)
        {
            if (_dicomTagPathParser.TryParse(attributeId, out DicomTag[] result, supportMultiple: true))
            {
                dicomTags = result;
                return true;
            }

            dicomTags = null;
            return false;
        }

        private static QueryTag GetMatchingQueryTag(DicomTag[] dicomTags, string attributeId, IEnumerable<QueryTag> queryTags)
        {
            if (dicomTags.Length > 2)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.NestedSequencesNotSupported, attributeId));
            }

            QueryTag queryTag = queryTags.FirstOrDefault(item =>
            {
                return Enumerable.SequenceEqual(dicomTags, item.WorkitemQueryTagStoreEntry.PathTags);
            });

            if (queryTag == null)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedWorkitemSearchParameter, attributeId));
            }

            // Currently only 2 level of sequence tags are supported, so always taking the last element to create a new query tag
            var dicomTag = dicomTags.LastOrDefault();
            var entry = new WorkitemQueryTagStoreEntry(queryTag.WorkitemQueryTagStoreEntry.Key, dicomTag.GetPath(), dicomTag.GetDefaultVR().Code)
            {
                PathTags = Array.AsReadOnly(new DicomTag[] { dicomTag })
            };

            return new QueryTag(entry);
        }

        private QueryIncludeField ParseIncludeFields(IReadOnlyList<string> includeFields)
        {
            // Check if "all" is present as one of the values in IncludeField parameter.
            if (includeFields.Any(val => IncludeFieldValueAll.Equals(val, StringComparison.OrdinalIgnoreCase)))
            {
                if (includeFields.Count > 1)
                {
                    throw new QueryParseException(DicomCoreResource.InvalidIncludeAllFields);
                }

                return QueryIncludeField.AllFields;
            }

            var fields = new List<DicomTag>(includeFields.Count);
            foreach (string field in includeFields)
            {
                if (!TryParseDicomAttributeId(field, out DicomTag[] dicomTags))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.IncludeFieldUnknownAttribute, field));
                }

                if (dicomTags.Length > 1)
                {
                    throw new QueryParseException(DicomCoreResource.SequentialDicomTagsNotSupported);
                }

                // For now only first level tags are supported
                fields.Add(dicomTags[0]);
            }

            return new QueryIncludeField(fields);
        }
    }
}
