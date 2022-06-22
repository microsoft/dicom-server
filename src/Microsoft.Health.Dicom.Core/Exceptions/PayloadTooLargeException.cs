// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// The incoming payload exceeds configured limits.
/// </summary>
public class PayloadTooLargeException : DicomServerException
{
    protected PayloadTooLargeException(string message)
        : base(message)
    {
    }

    protected PayloadTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
