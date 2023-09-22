// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class PersonNameValidation : IElementValidation
{
    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        string value = dicomElement.GetFirstValueOrDefault<string>();
        string name = dicomElement.Tag.GetFriendlyName();
        DicomVR vr = dicomElement.ValueRepresentation;
        if (withLeniency)
        {
            value = value.TrimEnd('\0');
        }
        if (string.IsNullOrEmpty(value))
        {
            // empty values allowed
            return;
        }

        string[] groups = value.Split('=');
        if (groups.Length > 3)
        {
            throw new ElementValidationException(name, DicomVR.PN, ValidationErrorCode.PersonNameExceedMaxGroups);
        }

        foreach (string group in groups)
        {
            try
            {
                ElementMaxLengthValidation.Validate(group, 64, name, dicomElement.ValueRepresentation);
            }
            catch (ElementValidationException ex) when (ex.ErrorCode == ValidationErrorCode.ExceedMaxLength)
            {
                // Reprocess the exception to make more meaningful message
                throw new ElementValidationException(name, DicomVR.PN, ValidationErrorCode.PersonNameGroupExceedMaxLength);
            }

            if (ValidationUtils.ContainsControlExceptEsc(group))
            {
                throw new ElementValidationException(name, vr, ValidationErrorCode.InvalidCharacters);
            }
        }

        if (groups.Select(g => g.Split('^').Length).Any(l => l > 5))
        {
            throw new ElementValidationException(name, DicomVR.PN, ValidationErrorCode.PersonNameExceedMaxComponents);
        }
    }
}
