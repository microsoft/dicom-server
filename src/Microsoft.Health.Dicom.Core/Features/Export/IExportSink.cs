// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Represents a destination for export operations into which file may be copied.
/// </summary>
public interface IExportSink : IAsyncDisposable
{
    /// <summary>
    /// Occurs when a file fails to copy.
    /// </summary>
    event EventHandler<CopyFailureEventArgs> CopyFailure;

    /// <summary>
    /// Gets the URI for the resource in which export errors are recorded.
    /// </summary>
    /// <value>A URI for the list of errors.</value>
    Uri ErrorHref { get; }

    /// <summary>
    /// Asychronously initializes the sink for copying.
    /// </summary>
    /// <remarks>
    /// Initialization will create any resources needed for copying and can be used to assess
    /// whether ther sink has been configured correctly.
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the <see cref="InitializeAsync"/> operation.</returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    /// <exception cref="SinkInitializationFailureException">The sink failed to initialize.</exception>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously copies the given <paramref name="value"/> into the destination.
    /// </summary>
    /// <remarks>
    /// The <paramref name="value"/> may represent either a DICOM file or an error generated
    /// by a corresponding source. Files and errors are typically written to different locations
    /// within the destination.
    /// </remarks>
    /// <param name="value">The result of a previous read operation. May be an identifier or an error.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the <see cref="CopyAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property is <see langword="true"/> if the operation
    /// succeeded; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
    Task<bool> CopyAsync(ReadResult value, CancellationToken cancellationToken = default);
}
