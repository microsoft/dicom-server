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
            QueryResource resourceType,
            DicomQueryParameterIncludeField includeFields,
            bool fuzzyMatching,
            int limit,
            int offset,
            IReadOnlyCollection<DicomQueryFilterCondition> filterConditions)
        {
            QueryResource = resourceType;
            IncludeFields = includeFields;
            FuzzyMatching = fuzzyMatching;
            Limit = limit;
            Offset = offset;
            FilterConditions = filterConditions;

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
                return Limit > 0 && Limit <= DicomQueryLimit.MaxQueryResultCount ?
                    Limit : DicomQueryLimit.DefaultQueryResultCount;
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
