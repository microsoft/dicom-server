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
    public void Validate(DicomElement dicomElement)
    {
        DicomVR vr = dicomElement.ValueRepresentation;
        switch (vr.Code)
        {
            case DicomVRCode.DT:
                Validate(dicomElement, DicomValidation.ValidateDT, ValidationErrorCode.DateTimeIsInvalid);
                break;
            case DicomVRCode.IS:
                Validate(dicomElement, DicomValidation.ValidateIS, ValidationErrorCode.IntegerStringIsInvalid);
                break;
            case DicomVRCode.TM:
                Validate(dicomElement, DicomValidation.ValidateTM, ValidationErrorCode.TimeIsInvalid);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dicomElement));
        };
    }

    private static void Validate(DicomElement element, Action<string> validate, ValidationErrorCode errorCode)
    {
        string value = element.GetFirstValueOrDefault<string>();

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
