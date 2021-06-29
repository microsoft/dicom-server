// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        /// Adds the extended query tags into the store if they are not present.
        /// </summary>
        /// <param name="extendedQueryTagEntries">The extended query tag entries.</param>
        /// <param name="maxAllowedCount">The max allowed count.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous add operation. The value of its <see cref="Task{TResult}.Result"/>
        /// property contains the keys for the <paramref name="extendedQueryTagEntries"/> in the store.
        /// </returns>
        Task<IReadOnlyList<int>> AddExtendedQueryTagsAsync(
            IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries,
            int maxAllowedCount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get stored extended query tags from ExtendedQueryTagStore, if provided, by tagPath.
        /// </summary>
        /// <param name="path">Path associated with requested extended query tag formatted as it is stored internally.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Extended Query tag entry/entries with path, VR, level and status.</returns>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(string path = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get extended query tags by keys.
        /// </summary>
        /// <param name="tagKeys">The tag keys.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(IReadOnlyList<int> tagKeys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete extended query tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="vr">The VR code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">Indicates the tag should be deleted regardless of status.</param>
        /// <returns>The task.</returns>
        // TODO: Remove optional force parameter after final API design
        Task DeleteExtendedQueryTagAsync(string tagPath, string vr, bool force = false, CancellationToken cancellationToken = default);
    }
}
