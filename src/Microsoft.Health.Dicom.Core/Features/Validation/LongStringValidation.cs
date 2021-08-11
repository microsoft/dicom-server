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
    internal class LongStringValidation : ElementValidation
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

            ElementMaxLengthValidation.Validate(value, 64, name, DicomVR.LO);

            if (value.Contains("\\", StringComparison.OrdinalIgnoreCase) || value.ToCharArray().Any(IsControlExceptESC))
            {
                throw new DicomElementValidationException(ElementValidationErrorCode.ValueContainsInvalidCharacters, name, DicomVR.LO, DicomCoreResource.ValueContainsInvalidCharacter, value);
            }
        }
    }

}
