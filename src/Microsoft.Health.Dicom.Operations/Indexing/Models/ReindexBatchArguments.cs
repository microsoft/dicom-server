// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Operations.Indexing.Models
{
    /// <summary>
    ///  Represents input to <see cref="ReindexDurableFunction.ReindexBatchV2Async"/>
    /// </summary>
    public sealed class ReindexBatchArguments
    {
        /// <summary>
        /// Gets or sets the tag entries.
        /// </summary>
        public IReadOnlyCollection<ExtendedQueryTagStoreEntry> QueryTags { get; }

        /// <summary>
        /// Gets or sets the number of threads available for each batch.
        /// </summary>
        public int ThreadCount { get; } = 5;

        /// <summary>
        /// Gets or sets the inclusive watermark range.
        /// </summary>
        public WatermarkRange WatermarkRange { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReindexBatchArguments"/> class with the specified values.
        /// </summary>
        /// <param name="queryTags">The tag entries.</param>
        /// <param name="watermarkRange">The inclusive watermark range.</param>
        /// <param name="threadCount">The number of threads available for each batch.</param>
        /// <exception cref="ArgumentNullException"><paramref name="queryTags"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="threadCount"/> is less than <c>1</c>.
        /// </exception>
        public ReindexBatchArguments(
            IReadOnlyCollection<ExtendedQueryTagStoreEntry> queryTags,
            WatermarkRange watermarkRange,
            int threadCount)
        {
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));
            EnsureArg.IsGte(threadCount, 1, nameof(threadCount));

            QueryTags = queryTags;
            ThreadCount = threadCount;
            WatermarkRange = watermarkRange;
        }

        internal static ReindexBatchArguments FromOptions(
            IReadOnlyCollection<ExtendedQueryTagStoreEntry> queryTags,
            WatermarkRange watermarkRange,
            QueryTagIndexingOptions indexingOptions)
        {
            EnsureArg.IsNotNull(indexingOptions, nameof(indexingOptions));
            return new ReindexBatchArguments(queryTags, watermarkRange, indexingOptions.BatchThreadCount);
        }
    }
}
