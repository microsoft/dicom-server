// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Linq;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model
{
    public class DicomQueryOptions
    {
        public DicomQueryOptions(
            DicomQueryExpression queryExpression,
            QueryResourceType resourceType,
            string studyInstanceUID = null,
            string seriesInstanceUID = null)
        {
            QueryExpression = queryExpression;
            QueryResourceType = resourceType;
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
        }

        /// <summary>
        /// Query parameters object
        /// </summary>
        public DicomQueryExpression QueryExpression { get; }

        /// <summary>
        /// Study UID
        /// </summary>
        public string StudyInstanceUID { get; }

        /// <summary>
        /// Series UID
        /// </summary>
        public string SeriesInstanceUID { get; }

        /// <summary>
        /// Resource level used for query
        /// </summary>
        public QueryResourceType QueryResourceType { get; }

        public bool AnyFilterCondition
        {
            get
            {
                return !(string.IsNullOrEmpty(StudyInstanceUID)
                    && string.IsNullOrEmpty(SeriesInstanceUID)
                    && !QueryExpression.FilterConditions.Any());
            }
        }

        /// <summary>
        /// evaluted result count for this request
        /// </summary>
        public int EvaluatedLimit
        {
            get
            {
                return QueryExpression.Limit > 0 && QueryExpression.Limit <= DicomQueryConditionLimit.MaxQueryResultCount ?
                    QueryExpression.Limit : DicomQueryConditionLimit.DefaultQueryResultCount;
            }
        }
    }
}
