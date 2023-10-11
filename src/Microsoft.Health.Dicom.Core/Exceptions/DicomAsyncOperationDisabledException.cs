// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// Exception that is thrown when async operation is disabled.
/// </summary>
public class DicomAsyncOperationDisabledException : BadRequestException
{
    public DicomAsyncOperationDisabledException()
        : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DicomAsyncOperationDisabled))
    {
    }
}
