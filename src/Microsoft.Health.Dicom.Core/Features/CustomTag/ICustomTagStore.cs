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
        /// Get a requested custom tag from CustomTagStore by tagPath.
        /// </summary>
        /// <param name="path">Path associated with requested custom tag formatted as it is stored internally.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Custom tag entry with path, VR, level and status.</returns>
        Task<CustomTagEntry> GetCustomTagAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all stored custom tags from CustomTagStore.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Custom tag entries with path, VR, level and status.</returns>
        Task<IEnumerable<CustomTagEntry>> GetAllCustomTagsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag from CustomTagStore.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Result.</returns>
        Task DeleteCustomTagAsync(long key, CancellationToken cancellationToken = default);

        Task<CustomTagStoreEntry> GetCustomTagAsync(string tagPath, CancellationToken cancellationToken = default);

        Task StartDeleteCustomTagAsync(long tagKey, CancellationToken cancellationToken = default);

        Task CompleteDeleteCustomTagAsync(long tagKey, CancellationToken cancellationToken = default);

        Task<long> DeleteCustomTagStringIndexAsync(long tagKey, int top, CancellationToken cancellationToken = default);

        Task<long> DeleteCustomTagLongIndexAsync(long tagKey, int top, CancellationToken cancellationToken = default);

        Task<long> DeleteCustomTagDoubleIndexAsync(long tagKey, int top, CancellationToken cancellationToken = default);

        Task<long> DeleteCustomTagDateTimeIndexAsync(long tagKey, int top, CancellationToken cancellationToken = default);

        Task<long> DeleteCustomTagPersonNameIndexAsync(long tagKey, int top, CancellationToken cancellationToken = default);
    }
}
