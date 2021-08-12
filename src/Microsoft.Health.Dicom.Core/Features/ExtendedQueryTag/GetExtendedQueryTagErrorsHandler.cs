// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class GetExtendedQueryTagErrorsHandler : BaseHandler, IRequestHandler<GetExtendedQueryTagErrorsRequest, GetExtendedQueryTagErrorsResponse>
    {
        private readonly IExtendedQueryTagErrorsService _getExtendedQueryTagErrorsService;

        public GetExtendedQueryTagErrorsHandler(IAuthorizationService<DataActions> authorizationService, IExtendedQueryTagErrorsService getExtendedQueryTagErrorsService)
            : base(authorizationService)
        {
            _getExtendedQueryTagErrorsService = EnsureArg.IsNotNull(getExtendedQueryTagErrorsService, nameof(getExtendedQueryTagErrorsService));
        }

        public async Task<GetExtendedQueryTagErrorsResponse> Handle(GetExtendedQueryTagErrorsRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException(DataActions.Read);
            }

            return await _getExtendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(request.Path, cancellationToken);
        }
    }
}
