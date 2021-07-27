// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class LongStringValidation : DicomElementValidation
    {
        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            string value = dicomElement.Get<string>();
            string name = dicomElement.Tag.GetFriendlyName();
            Validate(value, name);
        }

        public static void Validate(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.Length > 64)
            {
                throw new DicomStringElementValidationException(ValidationErrorCode.ValueExceedMaxLength, name, value, DicomVR.LO, DicomCoreResource.ValueLengthExceeds64Characters);
            }

            if (value.Contains("\\", StringComparison.OrdinalIgnoreCase) || value.ToCharArray().Any(IsControlExceptESC))
            {
                throw new DicomStringElementValidationException(ValidationErrorCode.ValueContainsInvalidCharacters, name, value, DicomVR.LO, DicomCoreResource.ValueContainsInvalidCharacter);
            }
        }
    }

}
