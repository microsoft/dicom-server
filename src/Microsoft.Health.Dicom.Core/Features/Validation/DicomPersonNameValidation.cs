// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class DicomPersonNameValidation : DicomElementValidation
    {
        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            string value = dicomElement.Get<string>();
            string name = dicomElement.Tag.GetFriendlyName();
            if (string.IsNullOrEmpty(value))
            {
                // empty values allowed
                return;
            }

            var groups = value.Split('=');
            if (groups.Length > 3)
            {
                throw new DicomValueElementValidationException(ValidationErrorCode.PatientNameHasTooManyGroups, name, value, DicomVR.PN, DicomCoreResource.ValueExceedsAllowedGroups);
            }

            foreach (var group in groups)
            {
                if (group.Length > 64)
                {
                    throw new DicomValueElementValidationException(ValidationErrorCode.PatientNameGroupIsTooLong, name, value, DicomVR.PN, DicomCoreResource.ValueLengthExceeds64Characters);
                }

                if (group.ToCharArray().Any(IsControlExceptESC))
                {
                    throw new DicomValueElementValidationException(ValidationErrorCode.ValueContainsInvalidCharacters, name, value, DicomVR.PN, DicomCoreResource.ValueContainsInvalidCharacter);
                }
            }

            var groupcomponents = groups.Select(group => group.Split('^').Length);
            if (groupcomponents.Any(l => l > 5))
            {
                throw new DicomValueElementValidationException(ValidationErrorCode.PatientNameHasTooManyComponents, name, value, DicomVR.PN, DicomCoreResource.ValueExceedsAllowedComponents);
            }
        }
    }
}
