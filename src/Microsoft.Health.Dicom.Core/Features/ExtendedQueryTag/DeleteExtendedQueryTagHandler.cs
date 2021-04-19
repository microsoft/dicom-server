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
    public class DeleteExtendedQueryTagHandler : BaseHandler, IRequestHandler<DeleteExtendedQueryTagRequest, DeleteExtendedQueryTagResponse>
    {
        private readonly IDeleteExtendedQueryTagService _deleteExtendedQueryTagService;

        public DeleteExtendedQueryTagHandler(IAuthorizationService<DataActions> authorizationService, IDeleteExtendedQueryTagService deleteExtendedQueryTagService)
            : base(authorizationService)
        {
            EnsureArg.IsNotNull(deleteExtendedQueryTagService, nameof(deleteExtendedQueryTagService));
            _deleteExtendedQueryTagService = deleteExtendedQueryTagService;
        }

        public async Task<DeleteExtendedQueryTagResponse> Handle(DeleteExtendedQueryTagRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Admin, cancellationToken) != DataActions.Admin)
            {
                throw new UnauthorizedDicomActionException(DataActions.Admin);
            }

            await _deleteExtendedQueryTagService.DeleteExtendedQueryTagAsync(request.TagPath, cancellationToken);
            return new DeleteExtendedQueryTagResponse();
        }
    }
}
