// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.DataCleanup.Models;

public class DataCleanupBatchCreationArguments
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
    /// Gets or sets the start filter stamp
    /// </summary>
    public DateTimeOffset StartFilterTimeStamp { get; }

    /// <summary>
    /// Gets or sets the end filter stamp
    /// </summary>
    public DateTimeOffset EndFilterTimeStamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataCleanupBatchCreationArguments"/> class with the specified values.
    /// </summary>
    /// <param name="maxWatermark">The optional inclusive maximum watermark.</param>
    /// <param name="batchSize">The number of DICOM instances processed by a single activity.</param>
    /// <param name="maxParallelBatches">The maximum number of concurrent batches processed at a given time.</param>
    /// <param name="startFilterTimeStamp">Start filter stamp</param>
    /// <param name="endFilterTimeStamp">End filter stamp</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="batchSize"/> is less than <c>1</c>.</para>
    /// <para>-or-</para>
    /// <para><paramref name="maxParallelBatches"/> is less than <c>1</c>.</para>
    /// </exception>
    public DataCleanupBatchCreationArguments(long? maxWatermark, int batchSize, int maxParallelBatches, DateTimeOffset startFilterTimeStamp, DateTimeOffset endFilterTimeStamp)
    {
        EnsureArg.IsGte(batchSize, 1, nameof(batchSize));
        EnsureArg.IsGte(maxParallelBatches, 1, nameof(maxParallelBatches));
        EnsureArg.IsTrue(startFilterTimeStamp <= endFilterTimeStamp, nameof(startFilterTimeStamp));

        BatchSize = batchSize;
        MaxParallelBatches = maxParallelBatches;
        MaxWatermark = maxWatermark;
        StartFilterTimeStamp = startFilterTimeStamp;
        EndFilterTimeStamp = endFilterTimeStamp;
    }
}
