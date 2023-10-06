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
    protected virtual bool AllowNullOrEmpty => true;

    public void Validate(DicomElement dicomElement, ValidationLevel validationLevel = ValidationLevel.Default)
    {
        EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));

        string name = dicomElement.Tag.GetFriendlyName();
        string value = GetValueOrDefault(dicomElement);

        if (!string.IsNullOrEmpty(value) && validationLevel == ValidationLevel.Default)
            value = value.TrimEnd('\0');

        // By default we will allow null or empty string and not go further with validation
        if (AllowNullOrEmpty && string.IsNullOrEmpty(value))
            return;

        ValidateStringElement(name, dicomElement.ValueRepresentation, value, dicomElement.Buffer);
    }

    protected abstract void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer);

    protected virtual string GetValueOrDefault(DicomElement dicomElement)
    {
        return dicomElement.GetFirstValueOrDefault<string>();
    }
}