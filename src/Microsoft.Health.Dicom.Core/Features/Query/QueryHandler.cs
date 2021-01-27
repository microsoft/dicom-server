// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryHandler : BaseHandler, IRequestHandler<QueryResourceRequest, QueryResourceResponse>
    {
        private readonly IQueryService _queryService;

        public QueryHandler(IDicomAuthorizationService dicomAuthorizationService, IQueryService queryService)
            : base(dicomAuthorizationService)
        {
            _queryService = EnsureArg.IsNotNull(queryService, nameof(queryService));
        }

        public async Task<QueryResourceResponse> Handle(QueryResourceRequest message, CancellationToken cancellationToken)
        {
            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException(DataActions.Read);
            }

            return await _queryService.QueryAsync(message, cancellationToken);
        }
    }
}
