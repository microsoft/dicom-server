// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.State
{
    /// <summary>
    /// Read and persistent SyncState in a data store.
    /// </summary>
    public interface ISyncStateStore
    {
        /// <summary>
        /// Read <see cref="SyncState"/> from the data store.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="SyncState"> representing last successful sync.</see>/></returns>
        Task<SyncState> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Persist <see cref="SyncState"/> with updates from the latest successfull sync.
        /// </summary>
        /// <param name="state">State representing the last successful sync.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync(SyncState state, CancellationToken cancellationToken = default);
    }
}
