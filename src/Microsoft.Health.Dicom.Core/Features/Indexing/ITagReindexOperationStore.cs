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
    public interface ITagReindexOperationStore
    {
        /// <summary>
        /// Get entires of operations.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The entries.</returns>
        Task<IReadOnlyList<TagReindexOperationEntry>> GetEntriesOfOperationAsync(
            string operationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///  Get next watermarks for operation.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="count">The count of watermarks</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Watermark list.</returns>
        Task<IReadOnlyList<long>> GetNextWatermarksOfOperationAsync(string operationId, int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update endwatermark of opeation.
        /// </summary>
        /// <param name="operationId">The operation id.</param>
        /// <param name="endWatermark">The end watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task UpdateEndWatermarkOfOperationAsync(string operationId, long endWatermark, CancellationToken cancellationToken = default);

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
