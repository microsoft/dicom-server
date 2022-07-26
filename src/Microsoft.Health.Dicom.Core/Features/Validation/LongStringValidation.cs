// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// Validate Dicom VR LO
/// </summary>
internal class LongStringValidation : IElementValidation
{
    public ValidationWarnings Validate(DicomElement dicomElement)
    {
        string value = dicomElement.GetFirstValueOrDefault<string>();
        string name = dicomElement.Tag.GetFriendlyName();
        Validate(value, name);
        return ValidationWarnings.None;
    }

    public static void Validate(string value, string name)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        ElementMaxLengthValidation.Validate(value, 64, name, DicomVR.LO);

        if (value.Contains('\\', StringComparison.OrdinalIgnoreCase) || ValidationUtils.ContainsControlExceptEsc(value))
        {
            throw new ElementValidationException(name, DicomVR.LO, ValidationErrorCode.InvalidCharacters);
        }
    }
}

