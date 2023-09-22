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
    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        DicomVR vr = dicomElement.ValueRepresentation;
        switch (vr.Code)
        {
            case DicomVRCode.DT:
                Validate(dicomElement, DicomValidation.ValidateDT, ValidationErrorCode.DateTimeIsInvalid, withLeniency);
                break;
            case DicomVRCode.IS:
                Validate(dicomElement, DicomValidation.ValidateIS, ValidationErrorCode.IntegerStringIsInvalid, withLeniency);
                break;
            case DicomVRCode.TM:
                Validate(dicomElement, DicomValidation.ValidateTM, ValidationErrorCode.TimeIsInvalid, withLeniency);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dicomElement));
        };
    }

    private static void Validate(DicomElement element, Action<string> validate, ValidationErrorCode errorCode, bool withLeniency)
    {
        string value = element.GetFirstValueOrDefault<string>();

        if (withLeniency)
        {
            value = value.TrimEnd('\0');
        }

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
