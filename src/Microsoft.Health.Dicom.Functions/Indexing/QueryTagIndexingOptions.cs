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
        internal const string SectionName = "Indexing";

        /// <summary>
        /// Gets or sets the number of DICOM instances processed by a single activity.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int BatchSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum number of concurrent batches processed at a given time.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int MaxParallelBatches { get; set; } = 10;

        /// <summary>
        /// Gets the maximum number of DICOM instances that are processed concurrently
        /// across all activities for a single orchestration instance.
        /// </summary>
        public int MaxParallelCount => BatchSize * MaxParallelBatches;
    }
}
