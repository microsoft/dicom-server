// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// The store saving custom tags.
    /// </summary>
    public interface ICustomTagStore
    {
        /// <summary>
        /// Add custom tags into CustomTagStore
        /// Notes: once moved to job framework, the return will jobid, to save development effort, just return task for now.
        /// </summary>
        /// <param name="customTagEntries">The custom tag entries.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tag key.</returns>
        Task AddCustomTagsAsync(IEnumerable<CustomTagEntry> customTagEntries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get stored custom tags from CustomTagStore, if provided, by tagPath.
        /// </summary>
        /// <param name="path">Path associated with requested custom tag formatted as it is stored internally.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Custom tag entry/entries with path, VR, level and status.</returns>
        Task<IReadOnlyCollection<CustomTagEntry>> GetCustomTagsAsync(string path = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="vr">The VR code.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        Task DeleteCustomTagAsync(string tagPath, string vr, CancellationToken cancellationToken = default);
    }
}
