// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class EncodedStringElementValidation : StringElementValidation
{
    protected override void ValidateStringElement(string name, string value, DicomVR vr, IByteBuffer buffer)
    {
        switch (vr.Code)
        {
            case DicomVRCode.DT:
                Validate(name, value, vr, buffer, DicomValidation.ValidateDT, ValidationErrorCode.DateTimeIsInvalid);
                break;
            case DicomVRCode.IS:
                Validate(name, value, vr, buffer, DicomValidation.ValidateIS, ValidationErrorCode.IntegerStringIsInvalid);
                break;
            case DicomVRCode.TM:
                Validate(name, value, vr, buffer, DicomValidation.ValidateTM, ValidationErrorCode.TimeIsInvalid);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        };
    }

    protected override bool GetValue(DicomElement dicomElement, out string value)
    {
        value = dicomElement.GetFirstValueOrDefault<string>();
        return string.IsNullOrEmpty(value);
    }

    private static void Validate(string name, string value, DicomVR vr, IByteBuffer buffer, Action<string> validate, ValidationErrorCode errorCode)
    {
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
            throw new ElementValidationException(name, vr, errorCode);
        }
    }
}
