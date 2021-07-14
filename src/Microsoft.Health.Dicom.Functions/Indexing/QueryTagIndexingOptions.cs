// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the configuration for a "reindex" function.
    /// </summary>
    public class QueryTagIndexingOptions
    {
        internal const string ConfigurationSectionName = "Indexing";

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

    }
}
