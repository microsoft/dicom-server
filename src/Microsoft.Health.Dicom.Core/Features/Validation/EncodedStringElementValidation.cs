// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class EncodedStringElementValidation : ElementValidation
{
    public override void Validate(DicomElement element)
    {
        base.Validate(element);

        DicomVR vr = element.ValueRepresentation;
        switch (vr.Code)
        {
            case DicomVRCode.DT:
                Validate(element, DicomValidation.ValidateDT, ValidationErrorCode.DateTimeIsInvalid);
                break;
            case DicomVRCode.IS:
                Validate(element, DicomValidation.ValidateIS, ValidationErrorCode.IntegerStringIsInvalid);
                break;
            case DicomVRCode.TM:
                Validate(element, DicomValidation.ValidateTM, ValidationErrorCode.TimeIsInvalid);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        };
    }

    private static void Validate(DicomElement element, Action<string> validate, ValidationErrorCode errorCode)
    {
        string value = element.Get<string>();

        try
        {
            validate(value);
        }
        catch (DicomValidationException)
        {
            throw new ElementValidationException(element.Tag.GetFriendlyName(), element.ValueRepresentation, value, errorCode);
        }
    }
}
