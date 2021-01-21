// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagStore
    {
        /// <summary>
        /// Add Custom tag into CustomTagStore.
        /// </summary>
        /// <param name="path">The tag path.</param>
        /// <param name="vr">The tag VR.</param>
        /// <param name="level">The tag level.</param>
        /// <param name="status">The tag status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The custom tag store entry.</returns>
        Task<long> AddCustomTagAsync(string path, string vr, CustomTagLevel level, CustomTagStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update custom tag status.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="status">The tag status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Result.</returns>
        Task UpdateCustomTagStatusAsync(long key, CustomTagStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete custom tag from CustomTagStore.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Result.</returns>
        Task DeleteCustomTagAsync(long key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get latest instance.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The watermark of latest instance.</returns>
        Task<long?> GetLatestInstanceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get instances with watermark upto maxWatermark.
        /// </summary>
        /// <param name="maxWatermark">The max watermark</param>
        /// <param name="top">Top X instanances.</param>
        /// <param name="indexStatus">The index status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The instances.</returns>
        Task<IEnumerable<VersionedInstanceIdentifier>> GetInstancesInThePastAsync(long maxWatermark, int top, IndexStatus indexStatus = IndexStatus.Created, CancellationToken cancellationToken = default);
    }
}
