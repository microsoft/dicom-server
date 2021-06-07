// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Dicom.Core.Messages.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    // TODO: Check for data action
    public class OperationStatusHandler : IRequestHandler<OperationStatusRequest, OperationStatusResponse>
    {
        private readonly IOperationsService _service;

        public OperationStatusHandler(IOperationsService service)
        {
            EnsureArg.IsNotNull(service, nameof(service));
            _service = service;
        }

        public async Task<OperationStatusResponse> Handle(OperationStatusRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            return await _service.GetStatusAsync(request.Id, cancellationToken);
        }
    }
}
