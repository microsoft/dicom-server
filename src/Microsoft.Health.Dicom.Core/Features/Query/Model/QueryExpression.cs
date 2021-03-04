// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model
{
    /// <summary>
    /// Valid parsed object representing query parameters for a QIDO-RS request
    /// </summary>
    public class QueryExpression
    {
        public QueryExpression(
            QueryResource resourceType,
            QueryIncludeField includeFields,
            bool fuzzyMatching,
            int limit,
            int offset,
            IReadOnlyCollection<QueryFilterCondition> filterConditions,
            IReadOnlyCollection<CustomTagFilterDetails> queriedCustomTagFilterDetails = null)
        {
            QueryResource = resourceType;
            IncludeFields = includeFields;
            FuzzyMatching = fuzzyMatching;
            Limit = limit;
            Offset = offset;
            FilterConditions = filterConditions;
            QueriedCustomTagFilterDetails = queriedCustomTagFilterDetails ?? Array.Empty<CustomTagFilterDetails>();

            SetIELevel();
         }

        /// <summary>
        /// Query Resource type level
        /// </summary>
        public QueryResource QueryResource { get; }

        /// <summary>
        /// Resource level Study/Series
        /// </summary>
        public ResourceType IELevel { get; private set; }

        /// <summary>
        /// Dicom tags to include in query result
        /// </summary>
        public QueryIncludeField IncludeFields { get; }

        /// <summary>
        /// Filter details associated with the custom tags being queried.
        /// </summary>
        public IReadOnlyCollection<CustomTagFilterDetails> QueriedCustomTagFilterDetails { get; }

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
        public IReadOnlyCollection<QueryFilterCondition> FilterConditions { get; }

        /// <summary>
        /// Request query was empty
        /// </summary>
        public bool HasFilters
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
                return Limit > 0 && Limit <= QueryLimit.MaxQueryResultCount ?
                    Limit : QueryLimit.DefaultQueryResultCount;
            }
        }

        public bool IsInstanceIELevel()
        {
            return IELevel == ResourceType.Instance;
        }

        public bool IsSeriesIELevel()
        {
            return IELevel == ResourceType.Series;
        }

        public bool IsStudyIELevel()
        {
            return IELevel == ResourceType.Study;
        }

        private void SetIELevel()
        {
            switch (QueryResource)
            {
                case QueryResource.AllInstances:
                case QueryResource.StudyInstances:
                case QueryResource.StudySeriesInstances:
                    IELevel = ResourceType.Instance;
                    break;
                case QueryResource.AllSeries:
                case QueryResource.StudySeries:
                    IELevel = ResourceType.Series;
                    break;
                case QueryResource.AllStudies:
                    IELevel = ResourceType.Study;
                    break;
            }
        }
    }
}
