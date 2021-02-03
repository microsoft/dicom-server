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
        /// <returns>The task.</returns>
        Task AddCustomTagsAsync(IEnumerable<CustomTagEntry> customTagEntries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Custom tags.
        /// </summary>
        /// <param name="tagPath">The tag path. If not specified, return all custom tags.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The custom tag entry list.</returns>
        Task<IEnumerable<CustomTagEntry>> GetCustomTagsAsync(string tagPath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Start deleting custom tag.
        /// </summary>
        /// <remarks>It validates custom tag status, and update to deindexing if valid.</remarks>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The task.</returns>
        Task StartDeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Complete deleting custom tag.
        /// </summary>
        /// <remarks>It validates custom tag status, and remove if valid.</remarks>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task CompleteDeleteCustomTagAsync(string tagPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag indexed as string.
        /// </summary>
        /// <param name="tagPath">The tag path</param>
        /// <param name="maxDeleteRecord">The max records are deleted.</param>
        /// <param name="cancellationToken">The cancellatio token.</param>
        /// <returns>Number of deleted records.</returns>
        Task<long> DeleteCustomTagStringIndexAsync(string tagPath, int maxDeleteRecord, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag indexed as long.
        /// </summary>
        /// <param name="tagPath">The tag path</param>
        /// <param name="maxDeleteRecord">The max records are deleted.</param>
        /// <param name="cancellationToken">The cancellatio token.</param>
        /// <returns>Number of deleted records.</returns>
        Task<long> DeleteCustomTagLongIndexAsync(string tagPath, int maxDeleteRecord, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag indexed as double.
        /// </summary>
        /// <param name="tagPath">The tag path</param>
        /// <param name="maxDeleteRecord">The max records are deleted.</param>
        /// <param name="cancellationToken">The cancellatio token.</param>
        /// <returns>Number of deleted records.</returns>
        Task<long> DeleteCustomTagDoubleIndexAsync(string tagPath, int maxDeleteRecord, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag indexed as datetime.
        /// </summary>
        /// <param name="tagPath">The tag path</param>
        /// <param name="maxDeleteRecord">The max records are deleted.</param>
        /// <param name="cancellationToken">The cancellatio token.</param>
        /// <returns>Number of deleted records.</returns>
        Task<long> DeleteCustomTagDateTimeIndexAsync(string tagPath, int maxDeleteRecord, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag indexed as personName.
        /// </summary>
        /// <param name="tagPath">The tag path</param>
        /// <param name="maxDeleteRecord">The max records are deleted.</param>
        /// <param name="cancellationToken">The cancellatio token.</param>
        /// <returns>Number of deleted records.</returns>
        Task<long> DeleteCustomTagPersonNameIndexAsync(string tagPath, int maxDeleteRecord, CancellationToken cancellationToken = default);
    }
}
