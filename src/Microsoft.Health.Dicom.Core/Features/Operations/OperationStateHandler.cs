// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations;

/// <summary>
/// Represents a handler that encapsulates <see cref="IDicomOperationsClient.GetStateAsync"/>
/// to process instances of <see cref="OperationStateRequest"/>.
/// </summary>
public class OperationStateHandler : BaseHandler, IRequestHandler<OperationStateRequest, OperationStateResponse>
{
    private readonly IDicomOperationsClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStateHandler"/> class.
    /// </summary>
    /// <param name="authorizationService">A service for determining if a user is authorized.</param>
    /// <param name="client">A client for interacting with DICOM operations.</param>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public OperationStateHandler(IAuthorizationService<DataActions> authorizationService, IDicomOperationsClient client)
        : base(authorizationService)
        => _client = EnsureArg.IsNotNull(client, nameof(client));

    /// <summary>
    /// Invokes <see cref="IDicomOperationsClient.GetStateAsync"/> by forwarding the
    /// <see cref="OperationStateRequest.OperationId"/> and returns its response.
    /// </summary>
    /// <param name="request">A request for the state of a particular DICOM operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="Handle(OperationStateRequest, CancellationToken)"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the state of the operation
    /// based on the <paramref name="request"/>, if found; otherwise <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    public async Task<OperationStateResponse> Handle(OperationStateRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
        {
            throw new UnauthorizedDicomActionException(DataActions.Read);
        }

        OperationState<DicomOperation> state = await _client.GetStateAsync(request.OperationId, cancellationToken);
        return state != null ? new OperationStateResponse(state) : null;
    }
}
