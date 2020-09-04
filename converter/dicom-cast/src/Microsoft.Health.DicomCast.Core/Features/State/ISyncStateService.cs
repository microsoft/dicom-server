// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.State
{
    /// <summary>
    /// Service that supports CRUD on sync status
    /// Should be operated by a single thread.
    /// </summary>
    public interface ISyncStateService
    {
        /// <summary>
        /// Get the current sync state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>SyncState object with the details on current.</returns>
        Task<SyncState> GetSyncStateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the sync state after new dicom events have been processed successfully.
        /// </summary>
        /// <param name="newSyncState">Sync state represeting new processed state.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateSyncStateAsync(SyncState newSyncState, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset the sync state to process the dicom events from the begining.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ResetSyncStateAsync(CancellationToken cancellationToken = default);
    }
}
