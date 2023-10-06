// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

/// <summary>
/// Validate Dicom VR LO 
/// </summary>
internal class LongStringValidation : StringElementValidation
{
    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
    {
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

