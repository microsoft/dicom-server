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
using Microsoft.Health.Dicom.Core.Messages.Update;

namespace Microsoft.Health.Dicom.Core.Features.Update;

internal class UpdateInstanceHandler : BaseHandler, IRequestHandler<UpdateInstanceRequest, UpdateInstanceResponse>
{
    private readonly IUpdateInstanceOperationService _updateInstanceOperationService;

    public UpdateInstanceHandler(IAuthorizationService<DataActions> authorizationService, IUpdateInstanceOperationService updateInstanceOperationService)
        : base(authorizationService)
        => _updateInstanceOperationService = EnsureArg.IsNotNull(updateInstanceOperationService, nameof(updateInstanceOperationService));

    public async Task<UpdateInstanceResponse> Handle(UpdateInstanceRequest request, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(request, nameof(request));
        EnsureArg.IsNotNull(request.UpdateSpec, nameof(request.UpdateSpec));

        if (await AuthorizationService.CheckAccess(DataActions.Write, cancellationToken) != DataActions.Write)
            throw new UnauthorizedDicomActionException(DataActions.Write);

        return await _updateInstanceOperationService.QueueUpdateOperationAsync(request.UpdateSpec, cancellationToken);
    }
}
