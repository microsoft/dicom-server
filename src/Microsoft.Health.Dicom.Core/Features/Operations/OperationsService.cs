// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    public class OperationsService : IOperationsService
    {
        private readonly IDicomOperationsClient _client;
        internal static readonly ImmutableHashSet<OperationType> PublicOperationTypes = ImmutableHashSet.Create(OperationType.Reindex);

        public OperationsService(IDicomOperationsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            _client = client;
        }

        public async Task<OperationStatusResponse> GetStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(id, nameof(id));

            OperationStatusResponse response = await _client.GetStatusAsync(id, cancellationToken);
            return response != null && PublicOperationTypes.Contains(response.Type) ? response : null;
        }
    }
}
