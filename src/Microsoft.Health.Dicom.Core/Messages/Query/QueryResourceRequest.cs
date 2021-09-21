// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Messages.Query
{
    public class QueryResourceRequest : IRequest<QueryResourceResponse>
    {
        public QueryResourceRequest(
            IEnumerable<KeyValuePair<string, StringValues>> requestQuery,
            QueryResource resourceType,
            KnownQueryParams staticQueryParams,
            string studyInstanceUid = null,
            string seriesInstanceUid = null)
        {
            RequestQuery = EnsureArg.IsNotNull(requestQuery, nameof(requestQuery));
            StaticQueryParams = EnsureArg.IsNotNull(staticQueryParams, nameof(staticQueryParams));
            QueryResourceType = resourceType;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
        }

        public IEnumerable<KeyValuePair<string, StringValues>> RequestQuery { get; }

        public QueryResource QueryResourceType { get; }

        public KnownQueryParams StaticQueryParams { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }
    }
}
