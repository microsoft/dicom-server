// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// Stands for reindex operation.
    /// </summary>
    public class ReindexOperation
    {
        /// <summary>
        /// Gets or sets operation id.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets query tags store entries.
        /// </summary>
        public IReadOnlyList<ExtendedQueryTagStoreEntry> StoreEntries { get; set; }

        /// <summary>
        /// The inclusive start watermark.
        /// </summary>
        public long? StartWatermark { get; set; }

        /// <summary>
        /// The inclusive end watermark.
        /// </summary>
        public long? EndWatermark { get; set; }
    }
}
