// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Valid parsed object represeting query parameters for a QIDO-RS request
    /// </summary>
    public class DicomQueryExpression
    {
        public DicomQueryExpression(
            QueryResourceType resourceType,
            DicomQueryParameterIncludeField includeFields,
            bool fuzzyMatching,
            int limit,
            int offset,
            IReadOnlyCollection<DicomQueryFilterCondition> filterConditions)
        {
            QueryResourceType = resourceType;
            IncludeFields = includeFields;
            FuzzyMatching = fuzzyMatching;
            Limit = limit;
            Offset = offset;
            FilterConditions = filterConditions;
         }

        /// <summary>
        /// Resource type level
        /// </summary>
        public QueryResourceType QueryResourceType { get; }

        /// <summary>
        /// Dicom tags to include in query result
        /// </summary>
        public DicomQueryParameterIncludeField IncludeFields { get; }

        /// <summary>
        /// If true do Fuzzy matching of PN tag types
        /// </summary>
        public bool FuzzyMatching { get; }

        /// <summary>
        /// Query result count
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// Query result skip offset count
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// List of filter conditions to find the DICOM objects
        /// </summary>
        public IReadOnlyCollection<DicomQueryFilterCondition> FilterConditions { get; }

        /// <summary>
        /// Request query was empty
        /// </summary>
        public bool AnyFilters
        {
            get
            {
                return FilterConditions.Any();
            }
        }

        /// <summary>
        /// evaluted result count for this request
        /// </summary>
        public int EvaluatedLimit
        {
            get
            {
                return Limit > 0 && Limit <= DicomQueryConditionLimit.MaxQueryResultCount ?
                    Limit : DicomQueryConditionLimit.DefaultQueryResultCount;
            }
        }
    }
}
