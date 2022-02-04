// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Features.Query;

namespace Microsoft.Health.Dicom.Core.Messages.Workitem
{
    public class QueryWorkitemResourceRequest : IRequest<QueryWorkitemResourceResponse>
    {
        public QueryWorkitemResourceRequest(BaseQueryParameters parameters)
            => Parameters = EnsureArg.IsNotNull(parameters, nameof(parameters));

        public BaseQueryParameters Parameters { get; }
    }
}
