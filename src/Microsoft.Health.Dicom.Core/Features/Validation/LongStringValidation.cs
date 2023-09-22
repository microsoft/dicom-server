// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// Validate Dicom VR LO 
/// </summary>
internal class LongStringValidation : IElementValidation
{
    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        string value = dicomElement.GetFirstValueOrDefault<string>();
        if (withLeniency)
        {
            value = value.TrimEnd('\0');
        }
        string name = dicomElement.Tag.GetFriendlyName();
        Validate(value, name);
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

