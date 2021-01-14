// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface IGetCustomTagsService
    {
        /// <summary>
        /// Gets requested Custom Tag.
        /// </summary>
        /// <param name="tagPath">Path to the custom tag that is requested.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public Task<GetCustomTagResponse> GetCustomTagAsync(string tagPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all stored Custom Tags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public Task<GetAllCustomTagsResponse> GetAllCustomTagsAsync(CancellationToken cancellationToken = default);
    }
}
