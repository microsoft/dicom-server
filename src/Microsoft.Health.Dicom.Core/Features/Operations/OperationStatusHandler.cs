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

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    /// <summary>
    /// Represents a handler that encapsulates <see cref="IDicomOperationsClient.GetStatusAsync"/>
    /// to process instances of <see cref="OperationStatusRequest"/>.
    /// </summary>
    public class OperationStatusHandler : BaseHandler, IRequestHandler<OperationStatusRequest, OperationStatusResponse>
    {
        private readonly IDicomOperationsClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationStatusHandler"/> class.
        /// </summary>
        /// <param name="authorizationService">A service for determining if a user is authorized.</param>
        /// <param name="client">A client for interacting with DICOM operations.</param>
        /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
        public OperationStatusHandler(IAuthorizationService<DataActions> authorizationService, IDicomOperationsClient client)
            : base(authorizationService)
            => _client = EnsureArg.IsNotNull(client, nameof(client));

        /// <summary>
        /// Invokes <see cref="IDicomOperationsClient.GetStatusAsync"/> by forwarding the
        /// <see cref="OperationStatusRequest.OperationId"/> and returns its response.
        /// </summary>
        /// <param name="request">A request for the status of a particular DICOM operation.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="Handle(OperationStatusRequest, CancellationToken)"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the status of the operation
        /// based on the <paramref name="request"/>, if found; otherwise <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        public async Task<OperationStatusResponse> Handle(OperationStatusRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            if (await AuthorizationService.CheckAccess(DataActions.Read, cancellationToken) != DataActions.Read)
            {
                throw new UnauthorizedDicomActionException(DataActions.Read);
            }

            OperationStatus status = await _client.GetStatusAsync(request.OperationId, cancellationToken);
            return status != null ? new OperationStatusResponse(status) : null;
        }
    }
}
