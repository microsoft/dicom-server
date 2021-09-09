// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// The store saving extended query tags.
    /// </summary>
    public interface IExtendedQueryTagStore
    {
        /// <summary>
        /// Asynchronously adds the extended query tags into the store if they are not present.
        /// </summary>
        /// <param name="extendedQueryTagEntries">The extended query tag entries.</param>
        /// <param name="maxAllowedCount">The max allowed count.</param>
        /// <param name="ready">Optionally indicates whether the <paramref name="extendedQueryTagEntries"/> have been fully indexed.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous add operation. The value of its <see cref="Task{TResult}.Result"/>
        /// property contains the keys for the <paramref name="extendedQueryTagEntries"/> in the store.
        /// </returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount,
            bool ready = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get stored extended query tags from ExtendedQueryTagStore, if provided, by tagPath.
        /// </summary>
        /// <param name="path">Path associated with requested extended query tag formatted as it is stored internally.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Extended Query tag entry/entries with path, VR, level and status.</returns>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(string path = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets extended query tags by keys.
        /// </summary>
        /// <param name="tagKeys">The tag keys.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(IReadOnlyList<int> tagKeys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update QueryStatus of extended query tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="queryStatus">The query status.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The updated extended query tag.</returns>
        Task<ExtendedQueryTagStoreEntry> UpdateExtendedQueryTagQueryStatusAsync(string tagPath, QueryTagQueryStatus queryStatus, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets extended query tags assigned to the <paramref name="operationId"/>.
        /// </summary>
        /// <param name="operationId">The unique ID for the re-indexing operation.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="GetExtendedQueryTagsByOperationAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the set of query tags assigned
        /// to the <paramref name="operationId"/>.
        /// </returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsByOperationAsync(Guid operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes extended query tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="vr">The VR code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously assigns the given <paramref name="operationId"/> to the given tag keys.
        /// </summary>
        /// <param name="queryTagKeys">The keys for the extended query tags.</param>
        /// <param name="operationId">The unique ID for the re-indexing operation.</param>
        /// <param name="returnIfCompleted">Indicates whether completed tags should also be returned.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="AssignReindexingOperationAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the subset of query tags that were
        /// successfully assigned to the <paramref name="operationId"/>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="queryTagKeys"/> is empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="queryTagKeys"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AssignReindexingOperationAsync(
            IReadOnlyList<int> queryTagKeys,
            Guid operationId,
            bool returnIfCompleted = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously marks the re-indexing operation as complete for the given extended query tags.
        /// </summary>
        /// <param name="queryTagKeys">The keys for the extended query tags.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="CompleteReindexingAsync"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the set of query tags whose
        /// status was successfully updated to "complete."
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="queryTagKeys"/> is empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="queryTagKeys"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<IReadOnlyList<int>> CompleteReindexingAsync(IReadOnlyList<int> queryTagKeys, CancellationToken cancellationToken = default);
    }
}
