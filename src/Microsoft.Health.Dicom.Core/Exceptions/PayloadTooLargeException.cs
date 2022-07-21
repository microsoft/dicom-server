// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// The incoming payload exceeds configured limits.
/// </summary>
public class PayloadTooLargeException : DicomServerException
{
    public PayloadTooLargeException(long maxAllowedLength)
        : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.RequestLengthLimitExceeded, maxAllowedLength))
    {
    }
}
