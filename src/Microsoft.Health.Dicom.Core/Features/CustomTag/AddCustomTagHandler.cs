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
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class AddCustomTagHandler : BaseHandler, IRequestHandler<AddCustomTagRequest, AddCustomTagResponse>
    {
        private readonly ICustomTagService _customTagService;

        public AddCustomTagHandler(IDicomAuthorizationService dicomAuthorizationService, ICustomTagService customTagService)
            : base(dicomAuthorizationService)
        {
            _customTagService = EnsureArg.IsNotNull(customTagService, nameof(customTagService));
        }

        public async Task<AddCustomTagResponse> Handle(AddCustomTagRequest request, CancellationToken cancellationToken)
        {
            if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            {
                throw new UnauthorizedDicomActionException(DataActions.Write);
            }

            return await _customTagService.AddCustomTagAsync(request.CustomTags, cancellationToken);
        }
    }
}
