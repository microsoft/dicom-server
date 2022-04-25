// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Models.Common;

namespace Microsoft.Health.Dicom.Core.Features.Export;

/// <summary>
/// Provides data for the event <see cref="IExportSource.ReadFailure"/>.
/// </summary>
public sealed class ReadFailureEventArgs : EventArgs
{
    /// <summary>
    /// Gets the identifier for the DICOM file(s) that failed to be read.
    /// </summary>
    /// <value>An identifier representing a study, series, or SOP instance.</value>
    public DicomIdentifier Identifier { get; }

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    /// <value>An instance of the <see cref="Exception"/> class.</value>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadFailureEventArgs"/> class.
    /// </summary>
    /// <param name="identifier">An identifier for the DICOM file(s) that failed to be read.</param>
    /// <param name="exception">The exception raised by the read failure.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public ReadFailureEventArgs(DicomIdentifier identifier, Exception exception)
    {
        Identifier = identifier;
        Exception = EnsureArg.IsNotNull(exception, nameof(exception));
    }
}
