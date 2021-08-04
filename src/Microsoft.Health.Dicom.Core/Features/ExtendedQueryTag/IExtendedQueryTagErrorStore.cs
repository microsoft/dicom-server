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
    public interface IExtendedQueryTagErrorStore
    {
        /// <summary>
        /// Get extended query tags errors by tag path.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of Extended Query Tag Errors.</returns>
        Task<IReadOnlyList<ExtendedQueryTagError>> GetExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds an error for a specified Extended Query Tag.
        /// </summary>
        /// <param name="tagKey">TagKey of the extended query tag to which an error will be added.</param>
        /// <param name="createdTime">Time at which the error was created.</param>
        /// <param name="errorCode">Error code.</param>
        /// <param name="watermark">Watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tag key.</returns>
        public Task<int> AddExtendedQueryTagErrorAsync(
            int tagKey,
            int errorCode,
            long watermark,
            DateTime createdTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete extended query tags errors by tag path.
        /// </summary>
        /// <param name="tagPath">The tag path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Task.</returns>
        Task DeleteExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default);
    }
}
