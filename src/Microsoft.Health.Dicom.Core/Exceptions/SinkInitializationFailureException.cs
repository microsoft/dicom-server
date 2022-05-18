// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Export;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// Represents a failure to initialize an instance of <see cref="IExportSink"/>.
/// </summary>
public class SinkInitializationFailureException : ValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SinkInitializationFailureException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SinkInitializationFailureException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SinkInitializationFailureException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or <see langword="null"/>
    /// if no inner exception is specified.
    /// </param>
    public SinkInitializationFailureException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
