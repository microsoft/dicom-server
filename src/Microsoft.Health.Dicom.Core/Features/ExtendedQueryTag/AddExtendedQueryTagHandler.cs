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
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagHandler : BaseHandler, IRequestHandler<AddExtendedQueryTagRequest, AddExtendedQueryTagResponse>
    {
        private readonly IAddExtendedQueryTagService _addExtendedQueryTagService;

        public AddExtendedQueryTagHandler(IDicomAuthorizationService dicomAuthorizationService, IAddExtendedQueryTagService addExtendedQueryTagService)
            : base(dicomAuthorizationService)
        {
            EnsureArg.IsNotNull(addExtendedQueryTagService, nameof(addExtendedQueryTagService));
            _addExtendedQueryTagService = addExtendedQueryTagService;
        }

        public async Task<AddExtendedQueryTagResponse> Handle(AddExtendedQueryTagRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

            return await _addExtendedQueryTagService.AddExtendedQueryTagAsync(request.ExtendedQueryTags, cancellationToken);
        }
    }
}
