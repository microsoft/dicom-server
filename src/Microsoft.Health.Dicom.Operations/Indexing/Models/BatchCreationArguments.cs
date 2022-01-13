// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Operations.Indexing.Models
{
    /// <summary>
    /// Represents the options for creating batches for re-indexing.
    /// </summary>
    public sealed class BatchCreationArguments
    {
        /// <summary>
        /// Gets or sets the optional inclusive maximum watermark.
        /// </summary>
        public long? MaxWatermark { get; }

        /// <summary>
        /// Gets or sets the number of DICOM instances processed by a single activity.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent batches processed at a given time.
        /// </summary>
        public int MaxParallelBatches { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchCreationArguments"/> class with the specified values.
        /// </summary>
        /// <param name="maxWatermark">The optional inclusive maximum watermark.</param>
        /// <param name="batchSize">The number of DICOM instances processed by a single activity.</param>
        /// <param name="maxParallelBatches">The maximum number of concurrent batches processed at a given time.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="maxWatermark"/> is less than <c>1</c>.</para>
        /// <para>-or-</para>
        /// <para><paramref name="batchSize"/> is less than <c>1</c>.</para>
        /// <para>-or-</para>
        /// <para><paramref name="maxParallelBatches"/> is less than <c>1</c>.</para>
        /// </exception>
        public BatchCreationArguments(long? maxWatermark, int batchSize, int maxParallelBatches)
        {
            if (maxWatermark.HasValue)
            {
                EnsureArg.IsGte(maxWatermark.GetValueOrDefault(), 1, nameof(maxWatermark));
            }

            EnsureArg.IsGte(batchSize, 1, nameof(batchSize));
            EnsureArg.IsGte(maxParallelBatches, 1, nameof(maxParallelBatches));

            BatchSize = batchSize;
            MaxParallelBatches = maxParallelBatches;
            MaxWatermark = maxWatermark;
        }

        internal static BatchCreationArguments FromOptions(long? maxWatermark, QueryTagIndexingOptions indexingOptions)
        {
            EnsureArg.IsNotNull(indexingOptions, nameof(indexingOptions));
            return new BatchCreationArguments(maxWatermark, indexingOptions.BatchSize, indexingOptions.MaxParallelBatches);
        }
    }
}
