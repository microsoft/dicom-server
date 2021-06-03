// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Store that records relationship between extended query tag and reindex operation.
    /// </summary>
    public interface ITagOperationStore
    {
        /// <summary>
        /// Get entires of operations.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The entries.</returns>
        Task<IReadOnlyList<TagOperationEntry>> GetEntriesOfOperationAsync(
            string operationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update endwatermark of opeation.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="endWatermark">The end watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task UpdateEndWatermarkOfOperationAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<long>> GetWatermarksAsync(long startWatermark, long endWatermark, CancellationToken cancellationToken = default);

        /// <summary>
        /// Complete operation.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task CompleteOperationAsync(string operationId, CancellationToken cancellationToken = default);


        Task StartOperationAsync(string operationId, IEnumerable<ExtendedQueryTagStoreEntry> entries, CancellationToken cancellationToken = default);
    }
}
