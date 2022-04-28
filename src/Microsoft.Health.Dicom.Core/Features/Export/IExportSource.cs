// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.Export;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a source for export operations from which files may be read.
/// </summary>
public interface IExportSource : IAsyncEnumerable<ReadResult>, IAsyncDisposable
{
    /// <summary>
    /// Occurs when a study, series, or SOP instance fails to be read.
    /// </summary>
    event EventHandler<ReadFailureEventArgs> ReadFailure;

    /// <summary>
    /// Gets the configuration that represents the current state of the source.
    /// </summary>
    /// <value>A configuration that represents the source.</value>
    TypedConfiguration<ExportSourceType> Configuration { get; }

    /// <summary>
    /// Attempts to dequeue a subset of the source's elements such that a new source may
    /// be created from the resulting configuration that contains the dequeued batch.
    /// </summary>
    /// <remarks>
    /// Batches may contain more and less elements depending on how many files remain
    /// in the source and on the implementation.
    /// </remarks>
    /// <param name="size">The size of the desired batch.</param>
    /// <param name="batch">
    /// When this method returns, the value resulting batch, if there is any data left;
    /// otherwise, the default value for the type of the <paramref name="batch"/> parameter.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the source contains any elements; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than <c>1</c>.</exception>
    bool TryDequeueBatch(int size, out TypedConfiguration<ExportSourceType> batch);
}
