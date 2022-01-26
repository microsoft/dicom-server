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
    /// Main parser class that implements the flow and registration of the parsers
    /// </summary>
    public partial class QueryParser : IQueryParser
    {
        private readonly IDicomTagParser _dicomTagPathParser;

        private const string IncludeFieldValueAll = "all";

        private readonly static Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>> ValueParsers = new Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>>
        {
            { DicomVR.DA, ParseDateOrTimeTagValue },
            { DicomVR.DT, ParseDateOrTimeTagValue },
            { DicomVR.TM, ParseTimeTagValue },

            { DicomVR.UI, ParseStringTagValue },
            { DicomVR.LO, ParseStringTagValue },
            { DicomVR.SH, ParseStringTagValue },
            { DicomVR.PN, ParseStringTagValue },
            { DicomVR.CS, ParseStringTagValue },

            { DicomVR.AE, ParseStringTagValue },
            { DicomVR.AS, ParseStringTagValue },
            { DicomVR.DS, ParseStringTagValue },
            { DicomVR.IS, ParseStringTagValue },

            { DicomVR.SL, ParseLongTagValue },
            { DicomVR.SS, ParseLongTagValue },
            { DicomVR.UL, ParseLongTagValue },
            { DicomVR.US, ParseLongTagValue },

            { DicomVR.FL, ParseDoubleTagValue },
            { DicomVR.FD, ParseDoubleTagValue }
        };

        public const string DateTagValueFormat = "yyyyMMdd";

        public static readonly string[] DateTimeTagValueFormats =
        {
            "yyyyMMddHHmmss.FFFFFF",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddHH",
            "yyyyMMdd",
            "yyyyMM",
            "yyyy"
        };

        public static readonly string[] DateTimeTagValueWithOffsetFormats =
        {
            "yyyyMMddHHmmss.FFFFFFzzz",
            "yyyyMMddHHmmsszzz",
            "yyyyMMddHHmmzzz",
            "yyyyMMddHHzzz",
            "yyyyMMddzzz",
            "yyyyMMzzz",
            "yyyyzzz"
        };

        public QueryParser(IDicomTagParser dicomTagPathParser)
            => _dicomTagPathParser = EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));

        public QueryExpression Parse(QueryParameters parameters, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(parameters, nameof(parameters));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            // Update the list of query tags
            queryTags = GetQualifiedQueryTags(queryTags, parameters.QueryResourceType);

            List<string> erroneousTags = new List<string>();

            var filterConditions = new Dictionary<DicomTag, QueryFilterCondition>();
            foreach (KeyValuePair<string, string> filter in parameters.Filters)
            {
                // filter conditions with attributeId as key
                if (!ParseFilterCondition(filter, queryTags, parameters.FuzzyMatching, out QueryFilterCondition condition))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.UnknownQueryParameter, filter.Key));
                }

                if (condition.QueryTag.IsExtendedQueryTag && condition.QueryTag.ExtendedQueryTagStoreEntry.ErrorCount > 0)
                {
                    erroneousTags.Add(filter.Key);
                }

                if (!filterConditions.TryAdd(condition.QueryTag.Tag, condition))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.DuplicateAttribute, filter.Key));
                }
            }

            // add UIDs as filter conditions
            if (parameters.StudyInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new QueryTag(DicomTag.StudyInstanceUID), parameters.StudyInstanceUid);
                if (!filterConditions.TryAdd(DicomTag.StudyInstanceUID, condition))
                {
                    throw new QueryParseException(DicomCoreResource.DisallowedStudyInstanceUIDAttribute);
                }
            }

            if (parameters.SeriesInstanceUid != null)
            {
                var condition = new StringSingleValueMatchCondition(new QueryTag(DicomTag.SeriesInstanceUID), parameters.SeriesInstanceUid);
                if (!filterConditions.TryAdd(DicomTag.SeriesInstanceUID, condition))
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
                filterConditions.Values,
                erroneousTags);
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
            if (!TryParseDicomAttributeId(queryParameter.Key, out DicomTag[] dicomTags))
            {
                return false;
            }

            // QueryTag could be either core or extended query tag or workitem query tag.
            QueryTag queryTag = dicomTags.Length > 1 ? GetMatchingQueryTag(dicomTags, queryParameter.Key, queryTags) :
                GetMatchingQueryTag(dicomTags.FirstOrDefault(), queryParameter.Key, queryTags);

            // check if tag is disabled
            if (queryTag.IsExtendedQueryTag && queryTag.ExtendedQueryTagStoreEntry.QueryStatus == QueryStatus.Disabled)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.QueryIsDisabledOnAttribute, queryParameter.Key));
            }

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
            if (_dicomTagPathParser.TryParse(attributeId, out DicomTag[] result))
            {
                dicomTags = result;
                return true;
            }

            dicomTags = null;
            return false;
        }

        private static QueryTag GetMatchingQueryTag(DicomTag dicomTag, string attributeId, IEnumerable<QueryTag> queryTags)
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

        private static QueryTag GetMatchingQueryTag(DicomTag[] dicomTags, string attributeId, IEnumerable<QueryTag> queryTags)
        {
            if (dicomTags.Length != 2)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.NestedSequencesNotSupported, attributeId));
            }

            QueryTag queryTag = queryTags.FirstOrDefault(item =>
            {
                return Enumerable.SequenceEqual(dicomTags, item.WorkitemQueryTagStoreEntry.PathTags);
            });

            if (queryTag == null)
            {
                throw new QueryParseException(string.Format(DicomCoreResource.UnsupportedSearchParameter, attributeId));
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

                fields.Add(dicomTags.FirstOrDefault());
            }

            return new QueryIncludeField(fields);
        }
    }
}
