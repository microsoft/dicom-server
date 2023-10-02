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
        GetValue(dicomElement, out string value);
        if (!string.IsNullOrEmpty(value) && validationLevel == ValidationLevel.Default)
            value = value.TrimEnd('\0');

        ValidateStringElement(name, value, dicomElement.ValueRepresentation, dicomElement.Buffer);
    }

    protected abstract void ValidateStringElement(string name, string value, DicomVR vr, IByteBuffer buffer);

    protected abstract bool GetValue(DicomElement dicomElement, out string value);
}