// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Functions.Indexing.Models;

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
    /// Gets or sets the inclusive watermark range.
    /// </summary>
    public WatermarkRange WatermarkRange { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReindexBatchArguments"/> class with the specified values.
    /// </summary>
    /// <param name="queryTags">The tag entries.</param>
    /// <param name="watermarkRange">The inclusive watermark range.</param>
    /// <exception cref="ArgumentNullException"><paramref name="queryTags"/> is <see langword="null"/>.</exception>
    public ReindexBatchArguments(
        IReadOnlyCollection<ExtendedQueryTagStoreEntry> queryTags,
        WatermarkRange watermarkRange)
    {
        EnsureArg.IsNotNull(queryTags, nameof(queryTags));

        QueryTags = queryTags;
        WatermarkRange = watermarkRange;
    }
}
