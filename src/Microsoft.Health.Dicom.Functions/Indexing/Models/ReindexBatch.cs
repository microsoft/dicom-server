// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models;

/// <summary>
///  Represents input to <see cref="ReindexDurableFunction.ReindexBatchAsync"/>
/// </summary>
[Obsolete("Please use ReindexBatchArguments instead.")]
public class ReindexBatch
{
    /// <summary>
    /// Gets or sets the inclusive watermark range.
    /// </summary>
    public WatermarkRange WatermarkRange { get; set; }

    /// <summary>
    /// Gets or sets the tag entries.
    /// </summary>
    public IReadOnlyCollection<ExtendedQueryTagStoreEntry> QueryTags { get; set; }

    internal ReindexBatchArguments ToArguments(int threadCount)
        => new ReindexBatchArguments(QueryTags, WatermarkRange, threadCount);
}
