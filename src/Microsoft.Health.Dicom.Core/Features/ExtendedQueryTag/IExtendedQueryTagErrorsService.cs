// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// The service for interacting with extended query tag error store.
    /// </summary>
    public interface IExtendedQueryTagErrorsService
    {
        /// <summary>
        /// Asynchronously adds an error for a specified Extended Query Tag.
        /// </summary>
        /// <param name="tagKey">TagKey of the extended query tag to which an error will be added.</param>
        /// <param name="errorCode">The validation error code.</param>
        /// <param name="watermark">Watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task AddExtendedQueryTagErrorAsync(
            int tagKey,
            ValidationErrorCode errorCode,
            long watermark,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets errors for a specified Extended Query Tag.
        /// </summary>
        /// <param name="tagPath">Path to the extended query tag that is requested.</param>
        /// <param name="limit">The maximum number of results to retrieve.</param>
        /// <param name="offset">The offset from which to retrieve paginated results.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="limit"/> is less than <c>1</c></para>
        /// <para>-or-</para>
        /// <para><paramref name="offset"/> is less than <c>0</c>.</para>
        /// </exception>
        Task<GetExtendedQueryTagErrorsResponse> GetExtendedQueryTagErrorsAsync(string tagPath, int limit, int offset = 0, CancellationToken cancellationToken = default);
    }
}
