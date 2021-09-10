// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        Task<GetExtendedQueryTagErrorsResponse> GetExtendedQueryTagErrorsAsync(string tagPath, CancellationToken cancellationToken = default);
    }
}
