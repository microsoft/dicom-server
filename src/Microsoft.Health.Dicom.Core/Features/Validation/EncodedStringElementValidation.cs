// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class EncodedStringElementValidation : IElementValidation
{
    public void Validate(DicomElement dicomElement, ValidationStyle validationStyle = ValidationStyle.Strict)
    {
        DicomVR vr = dicomElement.ValueRepresentation;
        switch (vr.Code)
        {
            case DicomVRCode.DT:
                Validate(dicomElement, DicomValidation.ValidateDT, ValidationErrorCode.DateTimeIsInvalid, validationStyle);
                break;
            case DicomVRCode.IS:
                Validate(dicomElement, DicomValidation.ValidateIS, ValidationErrorCode.IntegerStringIsInvalid, validationStyle);
                break;
            case DicomVRCode.TM:
                Validate(dicomElement, DicomValidation.ValidateTM, ValidationErrorCode.TimeIsInvalid, validationStyle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dicomElement));
        };
    }

    private static void Validate(DicomElement element, Action<string> validate, ValidationErrorCode errorCode, ValidationStyle validationStyle = ValidationStyle.Strict)
    {
        string value = BaseStringSanitizer.Sanitize(element.GetFirstValueOrDefault<string>(), validationStyle);

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        try
        {
            validate(value);
        }
        catch (DicomValidationException)
        {
            throw new ElementValidationException(element.Tag.GetFriendlyName(), element.ValueRepresentation, errorCode);
        }
    }
}
