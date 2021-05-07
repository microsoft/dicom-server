// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Indexing.Models
{
    /// <summary>
    /// Represents the input for a given re-index function instance.
    /// </summary>
    public class ReindexInput
    {
        /// <summary>
        /// Gets or sets the inclusive starting watermark for the re-index job.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Gets or sets the exclusive ending watermark for the re-index job.
        /// </summary>
        public long End { get; set; }

        /// <summary>
        /// Gets or sets the number of DICOM instances processed a single worker node.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent batches processed at a given time.
        /// </summary>
        public int MaxParallelBatches { get; set; }
    }
}
