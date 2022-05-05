// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Copy.Models;

/// <summary>
///  Represents input to <see cref="CopyDurableFunction.CopyBatchAsync"/>
/// </summary>
public sealed class CopyBatchArguments
{
    /// <summary>
    /// Gets or sets the number of threads available for each batch.
    /// </summary>
    public int ThreadCount { get; } = 5;

    /// <summary>
    /// Gets or sets the inclusive watermark range.
    /// </summary>
    public WatermarkRange WatermarkRange { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyBatchArguments"/> class with the specified values.
    /// </summary>
    /// <param name="watermarkRange">The inclusive watermark range.</param>
    /// <param name="threadCount">The number of threads available for each batch.</param>    
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="threadCount"/> is less than <c>1</c>.
    /// </exception>
    public CopyBatchArguments(
        WatermarkRange watermarkRange,
        int threadCount)
    {
        EnsureArg.IsGte(threadCount, 1, nameof(threadCount));
        ThreadCount = threadCount;
        WatermarkRange = watermarkRange;
    }

    internal static CopyBatchArguments FromOptions(
        WatermarkRange watermarkRange,
        CopyOptions duplicationOptions)
    {
        EnsureArg.IsNotNull(duplicationOptions, nameof(duplicationOptions));
        return new CopyBatchArguments(watermarkRange, duplicationOptions.BatchThreadCount);
    }
}
