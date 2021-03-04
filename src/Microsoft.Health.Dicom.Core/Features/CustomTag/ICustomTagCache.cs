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
    /// Cache current custom tag entries.
    /// </summary>
    public interface ICustomTagCache
    {
        /// <summary>
        /// Get All CustomTags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>All custom tags.</returns>
        Task<IReadOnlyCollection<CustomTagEntry>> GetCustomTagsAsync(CancellationToken cancellationToken = default);
    }
}
