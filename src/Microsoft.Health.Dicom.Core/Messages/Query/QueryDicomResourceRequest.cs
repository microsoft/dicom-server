// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public class QueryDicomResourceRequest : IRequest<QueryDicomResourceResponse>
    {
        public QueryDicomResourceRequest(
            IEnumerable<KeyValuePair<string, StringValues>> requestQuery,
            ResourceType resourceType,
            string studyInstanceUID = null,
            string seriesUID = null)
        {
            RequestQuery = requestQuery;
            ResourceType = resourceType;
            StudyInstanceUID = studyInstanceUID;
            SeriesUID = seriesUID;
        }

        public IEnumerable<KeyValuePair<string, StringValues>> RequestQuery { get; }

        public ResourceType ResourceType { get; }

        public string StudyInstanceUID { get; }

        public string SeriesUID { get; }
    }
}
