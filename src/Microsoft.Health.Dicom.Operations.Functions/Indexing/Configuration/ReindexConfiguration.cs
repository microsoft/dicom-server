// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Operations.Functions.Indexing.Configuration
{
    /// <summary>
    /// Represents the configuration for a "reindex" function.
    /// </summary>
    public class ReindexConfiguration
    {
        /// <summary>
        /// Gets or sets the number of DICOM instances processed a single worker node.
        /// </summary>
        /// <remarks>
        /// The number of DICOM instances interacted upon concurrently is the product of
        /// <see cref="BatchSize"/> and <see cref="MaxParallelBatches"/>.
        /// </remarks>
        [Range(1, int.MaxValue)]
        public int BatchSize { get; set; } = 2;

        /// <summary>
        /// Gets or sets the maximum number of concurrent batches processed at a given time.
        /// </summary>
        /// <remarks>
        /// The number of DICOM instances interacted upon concurrently is the product of
        /// <see cref="BatchSize"/> and <see cref="MaxParallelBatches"/>.
        /// </remarks>
        [Range(1, int.MaxValue)]
        public int MaxParallelBatches { get; set; } = 2;

        ///// <summary>
        ///// Gets or sets the maximum error percentage for a query tag before automatically pausing
        ///// its re-indexing.
        ///// </summary>
        //[Range(0d, 1d)]
        //public double MaxErrorPercentage { get; set; } = 1;
    }
}
