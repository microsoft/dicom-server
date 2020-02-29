// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public class QueryDicomResourceRequest : IRequest<QueryDicomResourceResponse>
    {
        public QueryDicomResourceRequest(
            IQueryCollection requestQuery,
            ResourceType resourceType,
            string studyInstanceUID = null,
            string seriesUID = null)
        {
            RequestQuery = requestQuery;
            ResourceType = resourceType;
            StudyInstanceUID = studyInstanceUID;
            SeriesUID = seriesUID;
        }

        public IQueryCollection RequestQuery { get; }

        public ResourceType ResourceType { get; }

        public string StudyInstanceUID { get; }

        public string SeriesUID { get; }
    }
}
