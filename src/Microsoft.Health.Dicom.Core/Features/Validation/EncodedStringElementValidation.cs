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
    public void Validate(DicomElement dicomElement, ValidationLevel validationLevel = ValidationLevel.Strict)
    {
        DicomVR vr = dicomElement.ValueRepresentation;
        switch (vr.Code)
        {
            case DicomVRCode.DT:
                Validate(dicomElement, DicomValidation.ValidateDT, ValidationErrorCode.DateTimeIsInvalid, validationLevel);
                break;
            case DicomVRCode.IS:
                Validate(dicomElement, DicomValidation.ValidateIS, ValidationErrorCode.IntegerStringIsInvalid, validationLevel);
                break;
            case DicomVRCode.TM:
                Validate(dicomElement, DicomValidation.ValidateTM, ValidationErrorCode.TimeIsInvalid, validationLevel);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dicomElement));
        };
    }

    private static void Validate(DicomElement element, Action<string> validate, ValidationErrorCode errorCode, ValidationLevel validationLevel = ValidationLevel.Strict)
    {
        string value = BaseStringSanitizer.Sanitize(element.GetFirstValueOrDefault<string>(), validationLevel);

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
