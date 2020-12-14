// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface ICustomTagStore
    {
        /// <summary>
        /// Refresh customTags based on its version.
        /// Only refresh when given version is out of date.
        /// </summary>
        /// <param name="customTags">The customTags.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the custom tags has been refreshed, false otherwise</returns>
        Task<bool> TryRefreshCustomTags(out CustomTagList customTags, CancellationToken cancellationToken = default);
    }
}
