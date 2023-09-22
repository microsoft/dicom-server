// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class ElementMaxLengthValidation : IElementValidation
{
    public ElementMaxLengthValidation(int maxLength)
    {
        Debug.Assert(maxLength > 0, "MaxLength should be positive number.");
        MaxLength = maxLength;
    }

    public int MaxLength { get; }

    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        string value = dicomElement.GetFirstValueOrDefault<string>();
        if (withLeniency)
        {
            value = value.TrimEnd('\0');
        }
        Validate(value, MaxLength, dicomElement.Tag.GetFriendlyName(), dicomElement.ValueRepresentation);
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
