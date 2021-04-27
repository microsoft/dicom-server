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
        /// Add extended query tags into ExtendedQueryTagStore
        /// Notes: once moved to job framework, the return will jobid, to save development effort, just return task for now.
        /// </summary>
        /// <param name="extendedQueryTagEntries">The extended query tag entries.</param>
        /// <param name="maxCount">The max count.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tag key.</returns>
        Task AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTagEntries, int maxCount = 128, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get stored extended query tags from ExtendedQueryTagStore, if provided, by tagPath.
        /// </summary>
        /// <param name="path">Path associated with requested extended query tag formatted as it is stored internally.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Extended Query tag entry/entries with path, VR, level and status.</returns>
        Task<IReadOnlyList<ExtendedQueryTagStoreEntry>> GetExtendedQueryTagsAsync(string path = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete extended query tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="vr">The VR code.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        Task DeleteExtendedQueryTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default);
    }
}
