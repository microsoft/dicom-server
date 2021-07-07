// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Represents an Reindexer which reindexes DICOM instance.
    /// </summary>
    public interface IInstanceReindexer
    {
        /// <summary>
        /// Reindex DICOM instance of watermark on extended query tags.
        /// </summary>
        /// <param name="entries">Extended query tag store entries.</param>
        /// <param name="watermark">The watermark to DICOM instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task ReindexInstanceAsync(IReadOnlyCollection<ExtendedQueryTagStoreEntry> entries, long watermark, CancellationToken cancellationToken = default);
    }
}
