// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public interface IGetExtendedQueryTagsService
    {
        /// <summary>
        /// Gets requested Extended Query Tag.
        /// </summary>
        /// <param name="tagPath">Path to the extended query tag that is requested.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public Task<GetExtendedQueryTagResponse> GetExtendedQueryTagAsync(string tagPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all stored Extended Query Tags.
        /// </summary>
        /// <param name="limit">The maximum number of results to retrieve.</param>
        /// <param name="offset">The offset from which to retrieve paginated results.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="limit"/> is less than <c>1</c> or greater than <c>200</c></para>
        /// <para>-or-</para>
        /// <para><paramref name="offset"/> is less than <c>0</c>.</para>
        /// </exception>
        public Task<GetExtendedQueryTagsResponse> GetExtendedQueryTagsAsync(int limit, int offset = 0, CancellationToken cancellationToken = default);
    }
}
