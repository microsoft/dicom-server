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
using Microsoft.Health.Dicom.Core.Messages.Workitem;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

public sealed class RetrieveWorkitemRequestHandler : BaseHandler, IRequestHandler<RetrieveWorkitemRequest, RetrieveWorkitemResponse>
{
    private readonly IWorkitemService _workItemService;

    public RetrieveWorkitemRequestHandler(IAuthorizationService<DataActions> authorizationService, IWorkitemService workItemService)
        : base(authorizationService)
    {
        _workItemService = EnsureArg.IsNotNull(workItemService, nameof(workItemService));
    }

    public async Task<RetrieveWorkitemResponse> Handle(RetrieveWorkitemRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
        {
            throw new UnauthorizedDicomActionException(DataActions.Read);
        }

        return await _workItemService
            .ProcessRetrieveAsync(request.WorkitemInstanceUid, cancellationToken)
            .ConfigureAwait(false);
    }
}
