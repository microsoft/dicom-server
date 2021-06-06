// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    public class OperationsService : IOperationsService
    {
        private readonly IDicomOperationsHttpClientService _clientService;
        private static readonly ImmutableHashSet<OperationType> PublicOperationTypes = ImmutableHashSet.Create(OperationType.AddExtendedQueryTag);

        public OperationsService(IDicomOperationsHttpClientService clientService)
        {
            EnsureArg.IsNotNull(clientService, nameof(clientService));

            _clientService = clientService;
        }
        public async Task<OperationStatusResponse> GetStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            OperationStatusResponse response = await _clientService.GetStatusAsync(id, cancellationToken);
            return PublicOperationTypes.Contains(response.Type) ? response : new OperationStatusResponse();
        }
    }
}
