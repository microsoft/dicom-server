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

public class UpdateWorkitemRequestHandler : BaseHandler, IRequestHandler<UpdateWorkitemRequest, UpdateWorkitemResponse>
{
    private readonly IWorkitemService _workItemService;
    private readonly IWorkitemSerializer _workitemSerializer;

    public UpdateWorkitemRequestHandler(
        IAuthorizationService<DataActions> authorizationService,
        IWorkitemSerializer workitemSerializer,
        IWorkitemService workItemService)
        : base(authorizationService)
    {
        _workItemService = EnsureArg.IsNotNull(workItemService, nameof(workItemService));
        _workitemSerializer = workitemSerializer;
    }

    /// <inheritdoc />
    public async Task<UpdateWorkitemResponse> Handle(
        UpdateWorkitemRequest request,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        // Verify that the user has Write permissions.
        if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken).ConfigureAwait(false) != DataActions.Write)
        {
            throw new UnauthorizedDicomActionException(DataActions.Write);
        }

        // Validate that the Workitem UID is not empty and is valid.
        // Also validate that the request payload is not empty.
        // If transaction UID is passed, make sure it is also valid.
        request.Validate();

        return await _workItemService
            .ProcessUpdateAsync(request.DicomDataset, request.WorkitemInstanceUid, request.TransactionUid, cancellationToken)
            .ConfigureAwait(false);
    }
}
