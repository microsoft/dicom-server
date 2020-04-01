// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryHandler : IRequestHandler<QueryDicomResourceRequest, QueryDicomResourceResponse>
    {
        private readonly IDicomQueryService _queryService;

        public QueryHandler(IDicomQueryService queryService)
        {
            EnsureArg.IsNotNull(queryService, nameof(queryService));

            _queryService = queryService;
        }

        public async Task<QueryDicomResourceResponse> Handle(QueryDicomResourceRequest message, CancellationToken cancellationToken)
        {
            return await _queryService.QueryAsync(message, cancellationToken);
        }
    }
}
