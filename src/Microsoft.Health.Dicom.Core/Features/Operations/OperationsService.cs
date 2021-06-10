// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;

namespace Microsoft.Health.Dicom.Core.Features.Operations
{
    /// <summary>
    /// Represents a service that interacts with long-running DICOM operations.
    /// </summary>
    public class OperationsService : IOperationsService
    {
        private readonly IDicomOperationsClient _client;
        internal static readonly ImmutableHashSet<OperationType> PublicOperationTypes = ImmutableHashSet.Create(OperationType.Reindex);

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationsService"/> class.
        /// </summary>
        /// <param name="client">A client for interacting with long-running DICOM operations.</param>
        /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
        public OperationsService(IDicomOperationsClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            _client = client;
        }

        /// <inheritdoc/>
        public async Task<OperationStatusResponse> GetStatusAsync(string operationId, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(operationId, nameof(operationId));

            OperationStatusResponse response = await _client.GetStatusAsync(operationId, cancellationToken);
            return response != null && PublicOperationTypes.Contains(response.Type) ? response : null;
        }
    }
}
