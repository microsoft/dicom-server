// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.ContentLengthBackFill.Models;

public class BatchCreationArguments
{
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
    /// <param name="batchSize">The number of DICOM instances processed by a single activity.</param>
    /// <param name="maxParallelBatches">The maximum number of concurrent batches processed at a given time.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="batchSize"/> is less than <c>1</c>.</para>
    /// <para>-or-</para>
    /// <para><paramref name="maxParallelBatches"/> is less than <c>1</c>.</para>
    /// </exception>
    public BatchCreationArguments(int batchSize, int maxParallelBatches)
    {
        EnsureArg.IsGte(batchSize, 1, nameof(batchSize));
        EnsureArg.IsGte(maxParallelBatches, 1, nameof(maxParallelBatches));

        BatchSize = batchSize;
        MaxParallelBatches = maxParallelBatches;
    }
}
