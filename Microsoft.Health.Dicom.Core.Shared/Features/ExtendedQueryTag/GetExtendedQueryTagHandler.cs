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
    public class GetExtendedQueryTagHandler : BaseHandler, IRequestHandler<GetExtendedQueryTagRequest, GetExtendedQueryTagResponse>
    {
        private readonly IGetExtendedQueryTagsService _getExtendedQueryTagsService;

        public GetExtendedQueryTagHandler(IAuthorizationService<DataActions> authorizationService, IGetExtendedQueryTagsService getExtendedQueryTagsService)
            : base(authorizationService)
        {
            EnsureArg.IsNotNull(getExtendedQueryTagsService, nameof(getExtendedQueryTagsService));
            _getExtendedQueryTagsService = getExtendedQueryTagsService;
        }

        public async Task<GetExtendedQueryTagResponse> Handle(GetExtendedQueryTagRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException(DataActions.Read);
            }

            return await _getExtendedQueryTagsService.GetExtendedQueryTagAsync(request.ExtendedQueryTagPath, cancellationToken);
        }
    }
}
