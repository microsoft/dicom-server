// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

internal class PersonNameValidation : StringElementValidation
{
    protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
    {
        string[] groups = value.Split('=');
        if (groups.Length > 3)
        {
            throw new ElementValidationException(name, DicomVR.PN, ValidationErrorCode.PersonNameExceedMaxGroups);
        }

        foreach (string group in groups)
        {
            try
            {
                ElementMaxLengthValidation.Validate(group, 64, name, vr);
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
