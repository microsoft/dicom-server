// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    /// Represents a batch of DICOM instances to be re-indexed based on the given tags.
    /// </summary>
    public class ReindexBatch
    {
        /// <summary>
        /// Gets or sets the inclusive starting watermark.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets or sets the exclusive ending watermark.
        /// </summary>
        public long End { get; set; }

        /// <summary>
        /// Gets or sets the tags upon which to index.
        /// </summary>
        public IReadOnlyList<ExtendedQueryTagEntry> TagEntries { get; set; }
    }
}
