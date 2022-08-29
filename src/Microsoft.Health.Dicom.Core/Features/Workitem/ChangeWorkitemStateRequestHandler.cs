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

public class ChangeWorkitemStateRequestHandler : BaseHandler, IRequestHandler<ChangeWorkitemStateRequest, ChangeWorkitemStateResponse>
{
    private readonly IWorkitemService _workItemService;

    public ChangeWorkitemStateRequestHandler(
        IAuthorizationService<DataActions> authorizationService,
        IWorkitemService workItemService)
        : base(authorizationService)
    {
        _workItemService = EnsureArg.IsNotNull(workItemService, nameof(workItemService));
    }

    /// <inheritdoc />
    public async Task<ChangeWorkitemStateResponse> Handle(ChangeWorkitemStateRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken).ConfigureAwait(false) != DataActions.Write)
        {
            throw new UnauthorizedDicomActionException(DataActions.Write);
        }

        request.Validate();

        return await _workItemService
            .ProcessChangeStateAsync(request.DicomDataset, request.WorkitemInstanceUid, cancellationToken)
            .ConfigureAwait(false);
    }
}
