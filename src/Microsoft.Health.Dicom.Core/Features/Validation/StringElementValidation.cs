// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;


internal abstract class StringElementValidation : IElementValidation
{
    public void Validate(DicomElement dicomElement, ValidationLevel validationLevel = ValidationLevel.Default)
    {
        EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));

        string name = dicomElement.Tag.GetFriendlyName();
        GetValueOrDefault(dicomElement, out string value);
        if (!string.IsNullOrEmpty(value) && validationLevel == ValidationLevel.Default)
            value = value.TrimEnd('\0');

        if (IsNullOrEmpty(value))
        {
            // By default we will allow null or empty string and not go further with validation
            return;
        }

        ValidateStringElement(name, dicomElement.ValueRepresentation, value, dicomElement.Buffer);
    }

    protected abstract void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer);

    protected virtual void GetValueOrDefault(DicomElement dicomElement, out string value)
    {
        value = dicomElement.GetFirstValueOrDefault<string>();
    }

    protected virtual bool IsNullOrEmpty(string value)
    {
        return string.IsNullOrEmpty(value);
    }
}