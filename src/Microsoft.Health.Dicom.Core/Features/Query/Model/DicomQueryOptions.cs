// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model
{
    public class DicomQueryOptions
    {
        public DicomQueryOptions(
            DicomQueryExpression queryExpression,
            ResourceType resourceType,
            string studyInstanceUID = null,
            string seriesInstanceUID = null)
        {
            QueryExpression = queryExpression;
            ResourceType = resourceType;
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
        public ResourceType ResourceType { get; }

        /// <summary>
        /// Specific instance already specified in the queryParams
        /// </summary>
        public bool IsOnlyInstanceUIDQuery
        {
            get
            {
                return
                    QueryExpression.FilterConditions.Count == 1
                    && QueryExpression.FilterConditions.First().DicomTag == DicomTag.SOPInstanceUID;
            }
        }

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
