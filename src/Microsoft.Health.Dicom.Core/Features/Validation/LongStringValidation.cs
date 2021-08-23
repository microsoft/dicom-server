// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    /// <summary>
    /// Validate Dicom VR LO 
    /// </summary>
    internal class LongStringValidation : ElementValidation
    {
        private const int MaxLength = 64;
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

            ElementMaxLengthValidation.Validate(value, MaxLength, name, DicomVR.LO);

            if (value.Contains("\\", StringComparison.OrdinalIgnoreCase) || ContainsControlExceptEsc(value))
            {
                throw new DicomElementValidationException(name, DicomVR.LO, DicomCoreResource.ValueContainsInvalidCharacter, value.Truncate(MaxLength));
            }
        }
    }

}
