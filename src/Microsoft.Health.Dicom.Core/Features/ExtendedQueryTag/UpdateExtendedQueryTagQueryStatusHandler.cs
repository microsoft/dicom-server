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
    public class UpdateExtendedQueryTagQueryStatusHandler : BaseHandler, IRequestHandler<UpdateExtendedQueryTagQueryStatusRequest, UpdateExtendedQueryTagQueryStatusResponse>
    {
        private readonly IUpdateExtendedQueryTagService _updateTagService;

        public UpdateExtendedQueryTagQueryStatusHandler(IAuthorizationService<DataActions> authorizationService, IUpdateExtendedQueryTagService updateTagService)
            : base(authorizationService)
        {
            _updateTagService = EnsureArg.IsNotNull(updateTagService, nameof(updateTagService));
        }

        public async Task<UpdateExtendedQueryTagQueryStatusResponse> Handle(UpdateExtendedQueryTagQueryStatusRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.ManageExtendedQueryTags, cancellationToken) != DataActions.ManageExtendedQueryTags)
            {
                throw new UnauthorizedDicomActionException(DataActions.ManageExtendedQueryTags);
            }

            var tagEntry = await _updateTagService.UpdateQueryStatusAsync(request.TagPath, request.QueryStatus, cancellationToken);
            return new UpdateExtendedQueryTagQueryStatusResponse(tagEntry);
        }
    }
}
