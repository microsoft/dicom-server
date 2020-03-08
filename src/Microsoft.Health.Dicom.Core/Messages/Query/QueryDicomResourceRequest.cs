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
            QueryResourceType resourceType,
            string studyInstanceUID = null,
            string seriesInstanceUID = null)
        {
            RequestQuery = requestQuery;
            QueryResourceType = resourceType;
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
        }

        public IEnumerable<KeyValuePair<string, StringValues>> RequestQuery { get; }

        public QueryResourceType QueryResourceType { get; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }
    }
}
