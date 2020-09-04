// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.DicomCast.Core.Features.State
{
    /// <inheritdoc/>
    public class SyncStateService : ISyncStateService
    {
        private readonly ISyncStateStore _store;

        public SyncStateService(ISyncStateStore store)
        {
            EnsureArg.IsNotNull(store);

            _store = store;
        }

        /// <inheritdoc/>
        public async Task<SyncState> GetSyncStateAsync(CancellationToken cancellationToken)
        {
            return await _store.ReadAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateSyncStateAsync(SyncState newSyncState, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(newSyncState);

            await _store.UpdateAsync(newSyncState, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ResetSyncStateAsync(CancellationToken cancellationToken)
        {
            await _store.UpdateAsync(SyncState.CreateInitialSyncState(), cancellationToken);
        }
    }
}
