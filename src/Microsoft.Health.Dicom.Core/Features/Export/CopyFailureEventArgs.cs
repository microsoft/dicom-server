// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Provides data for the event <see cref="IExportSink.CopyFailure"/>.
/// </summary>
public sealed class CopyFailureEventArgs : EventArgs
{
    /// <summary>
    /// Gets the identifier for the DICOM file that failed to copy.
    /// </summary>
    /// <value>The versioned instance identifier including its watermark.</value>
    public VersionedInstanceIdentifier Identifier { get; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    /// <value>An instance of the <see cref="Exception"/> class.</value>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyFailureEventArgs"/> class.
    /// </summary>
    /// <param name="identifier">An identifier for the DICOM file that failed to copy.</param>
    /// <param name="exception">The exception raised by the copy failure.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="identifier"/> or <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public CopyFailureEventArgs(VersionedInstanceIdentifier identifier, Exception exception)
    {
        Identifier = EnsureArg.IsNotNull(identifier, nameof(identifier));
        Exception = EnsureArg.IsNotNull(exception, nameof(exception));
    }
}
