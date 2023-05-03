// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// The exception that is thrown when a Dicom update operation request is submitted while one is already active.
/// </summary>
public sealed class ExistingUpdateOperationException : Exception
{
    /// <summary>
    /// Gets the reference to the existing update operation.
    /// </summary>
    /// <value>The <see cref="OperationReference"/> for the existing operation, if specified.</value>
    public OperationReference ExistingOperation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExistingUpdateOperationException"/> class.
    /// </summary>
    public ExistingUpdateOperationException()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExistingUpdateOperationException"/> class.
    /// </summary>
    /// <param name="operation">The operation reference for the existing update operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public ExistingUpdateOperationException(OperationReference operation)
        : base(
            string.Format(
                CultureInfo.CurrentCulture,
                DicomCoreResource.ExistingUpdateOperation,
                EnsureArg.IsNotNull(operation, nameof(operation)).Id.ToString(OperationId.FormatSpecifier)))
    {
        ExistingOperation = operation;
    }
}
