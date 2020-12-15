// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Cache for custom tag list.
    /// </summary>
    public interface ICustomTagListCache
    {
        /// <summary>
        /// Get Custom Tags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The custom tags.</returns>
        Task<CustomTagList> GetCustomTagsAsync(CancellationToken cancellationToken = default);
    }
}
