// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class ElementMaxLengthValidation : StringElementValidation
{
    public ElementMaxLengthValidation(int maxLength)
    {
        Debug.Assert(maxLength > 0, "MaxLength should be positive number.");
        MaxLength = maxLength;
    }

    public int MaxLength { get; }

    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
    {
        Validate(value, MaxLength, name, vr);
    }

    public static void Validate(string value, int maxLength, string name, DicomVR vr)
    {
        EnsureArg.IsNotNullOrEmpty(name, nameof(name));
        EnsureArg.IsNotNull(vr, nameof(vr));
        if (value?.Length > maxLength)
        {
            throw new ElementValidationException(
                name,
                vr,
                ValidationErrorCode.ExceedMaxLength,
                string.Format(CultureInfo.CurrentCulture, DicomCoreResource.ErrorMessageExceedMaxLength, maxLength));
        }
    }
}
