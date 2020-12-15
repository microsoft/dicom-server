// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// The store for saving custom tags.
    /// </summary>
    public interface ICustomTagStore
    {
        /// <summary>
        /// Refresh given CustomTagList.
        /// If given customtags is latest, no action is taken. Otherwise, update given customTags from store.
        /// </summary>
        /// <param name="customTags">The customTags.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the custom tags has been refreshed, false otherwise</returns>
        Task<bool> TryRefreshCustomTags(out CustomTagList customTags, CancellationToken cancellationToken = default);
    }
}
