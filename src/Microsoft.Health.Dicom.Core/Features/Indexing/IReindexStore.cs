// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Store that records reindex status.
    /// </summary>
    public interface IReindexStore
    {
        /// <summary>
        /// Get entries of reindex.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The entries.</returns>
        Task<IEnumerable<ReindexEntry>> GetReindexEntriesAsync(string operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update reindex progress.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="endWatermark">The end watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task UpdateReindexProgressAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default);

        /// <summary>
        /// Complete reindex.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task CompleteReindexAsync(string operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Start reindex.
        /// </summary>
        /// <param name="tagKeys">Key to tags</param>
        /// <param name="operationId">The operation id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task<ReindexOperation> PrepareReindexingAsync(IReadOnlyList<int> tagKeys, string operationId, CancellationToken cancellationToken = default);
    }
}
