// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public interface IReindexJob
    {
        /// <summary>
        /// Reindex through ealiest instance till endWatermark (include endWatermark) on given customTags.
        /// </summary>
        /// <param name="customTags">The custom tags.</param>
        /// <param name="endWatermark">The end watermark.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        Task ReindexAsync(IEnumerable<CustomTagEntry> customTags, long endWatermark, CancellationToken cancellationToken = default);
    }
}
