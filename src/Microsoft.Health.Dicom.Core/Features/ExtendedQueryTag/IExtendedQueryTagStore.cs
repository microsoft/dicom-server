// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

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
    /// property contains the added extended query tags.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> AddExtendedQueryTagsAsync(
        IReadOnlyCollection<AddExtendedQueryTagEntry> extendedQueryTagEntries,
        int maxAllowedCount,
        bool ready = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the stored extended query tag from ExtendedQueryTagStore by its path.
    /// </summary>
    /// <param name="tagPath">Path associated with requested extended query tag formatted as it is stored internally.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains the tag's information as found in storage.
    /// </returns>
    Task<ExtendedQueryTagStoreJoinEntry> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stored extended query tags from ExtendedQueryTagStore, if provided, by tagPath.
    /// </summary>
    /// <param name="limit">The maximum number of results to retrieve.</param>
    /// <param name="offset">The offset from which to retrieve paginated results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous get operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains a list of the tags' information as found in storage.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="limit"/> is less than <c>1</c></para>
    /// <para>-or-</para>
    /// <para><paramref name="offset"/> is less than <c>0</c>.</para>
    /// </exception>
    Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(int limit, long offset = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets extended query tags by keys.
    /// </summary>
    /// <param name="queryTagKeys">The tag keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task<IReadOnlyList<ExtendedQueryTagStoreJoinEntry>> GetExtendedQueryTagsAsync(IReadOnlyCollection<int> queryTagKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update QueryStatus of extended query tag.
    /// </summary>
    /// <param name="tagPath">The tag path.</param>
    /// <param name="queryStatus">The query status.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated extended query tag.</returns>
    Task<ExtendedQueryTagStoreJoinEntry> UpdateQueryStatusAsync(string tagPath, QueryStatus queryStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets extended query tags assigned to the <paramref name="operationId"/>.
    /// </summary>
    /// <param name="operationId">The unique ID for the re-indexing operation.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="GetExtendedQueryTagsAsync(Guid, CancellationToken)"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains the set of query tags assigned
    /// to the <paramref name="operationId"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes extended query tag.
    /// </summary>
    /// <param name="tagPath">The tag path.</param>
    /// <param name="vr">The VR code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the extended query tag entry
    /// </summary>
    /// <param name="tagKey">The key of the tag to delete</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the <see cref="DeleteExtendedQueryTagEntryAsync"/> operation.</returns>
    Task DeleteExtendedQueryTagEntryAsync(int tagKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates the status of the extended query tag to 'Deleting'
    /// </summary>
    /// <param name="tagKey">The key of the tag to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the <see cref="DeleteExtendedQueryTagEntryAsync"/> operation.</returns>
    /// <exception cref="ExtendedQueryTagBusyException">The extended query tag is already in a Deleting state.</exception>
    /// <exception cref="ExtendedQueryTagNotFoundException">The extended query tag could not be found.</exception>
    Task UpdateExtendedQueryTagStatusToDelete(int tagKey, CancellationToken cancellationToken = default);

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
        IReadOnlyCollection<int> queryTagKeys,
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
    Task<IReadOnlyList<int>> CompleteReindexingAsync(IReadOnlyCollection<int> queryTagKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets batches of extended query tag indicies based on the watermark
    /// </summary>
    /// <param name="batchSize">The size of the batch of each watermark range</param>
    /// <param name="batchCount">The maximum number of watermark ranges to create</param>
    /// <param name="vr">The VR of the tag</param>
    /// <param name="tagKey">The key of the tag</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A list of watermark ranges that define equal batches of work to act on.</returns>
    Task<IReadOnlyList<WatermarkRange>> GetExtendedQueryTagBatches(int batchSize, int batchCount, string vr, int tagKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously deletes the extended query tag index in the specified watermark range
    /// </summary>
    /// <param name="startWatermark">The watermark to start deleting at.</param>
    /// <param name="endWatermark">The watermark to finish deleting at.</param>
    /// <param name="vr">The VR of the tag</param>
    /// <param name="tagKey">The key of the tag</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A task representing the <see cref="DeleteExtendedQueryTagDataByWatermarkRangeAsync"/> operation.</returns>
    Task DeleteExtendedQueryTagDataByWatermarkRangeAsync(long startWatermark, long endWatermark, string vr, int tagKey, CancellationToken cancellationToken = default);
}
