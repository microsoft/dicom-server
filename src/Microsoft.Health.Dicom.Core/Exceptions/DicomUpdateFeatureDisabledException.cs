// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Dicom.Core.Exceptions;

/// <summary>
/// Exception that is thrown when Dicom update feature is disabled.
/// </summary>
public class DicomUpdateFeatureDisabledException : BadRequestException
{
    public DicomUpdateFeatureDisabledException()
        : base(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.DicomUpdateFeatureDisabled))
    {
    }
}
