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
/// The exception that is thrown when a re-index request is submitted while one is already active.
/// </summary>
public sealed class ExistingReindexException : ExistingOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExistingReindexException"/> class.
    /// </summary>
    /// <param name="operation">The operation reference for the existing re-index operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public ExistingReindexException(OperationReference operation)
        : base(
            operation,
            string.Format(
                CultureInfo.CurrentCulture,
                DicomCoreResource.ExistingReindexOperation,
                EnsureArg.IsNotNull(operation, nameof(operation)).Id.ToString(OperationId.FormatSpecifier)))
    { }
}
