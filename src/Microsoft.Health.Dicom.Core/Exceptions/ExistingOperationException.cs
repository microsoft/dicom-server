// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using System.Globalization;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// The exception that is thrown when a new operation is submitted while one is already active.
/// </summary>
public class ExistingOperationException : Exception
{
    /// <summary>
    /// Gets the reference to the existing operation.
    /// </summary>
    /// <value>The <see cref="OperationReference"/> for the existing operation, if specified.</value>
    public OperationReference ExistingOperation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExistingOperationException"/> class.
    /// </summary>
    /// <param name="operation">The operation reference for the existing operation.</param>
    /// <param name="operationType">Type of operation (eg: update, re-index)</param>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public ExistingOperationException(OperationReference operation, string operationType)
        : base(string.Format(
                CultureInfo.CurrentCulture,
                DicomCoreResource.ExistingOperation,
                operationType,
                EnsureArg.IsNotNull(operation, nameof(operation)).Id.ToString(OperationId.FormatSpecifier)))
    {
        ExistingOperation = operation;
    }
}
