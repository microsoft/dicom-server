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
            QueryResource resourceType,
            string studyInstanceUid = null,
            string seriesInstanceUid = null)
        {
            RequestQuery = requestQuery;
            QueryResourceType = resourceType;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
        }

        public IEnumerable<KeyValuePair<string, StringValues>> RequestQuery { get; }

        public QueryResource QueryResourceType { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }
    }
}
