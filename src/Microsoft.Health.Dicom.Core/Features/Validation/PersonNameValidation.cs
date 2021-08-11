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
    internal class PersonNameValidation : ElementValidation
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
                throw new DicomElementValidationException(ElementValidationErrorCode.PatientNameHasTooManyGroups, name, DicomVR.PN, DicomCoreResource.ValueExceedsAllowedGroups, value);
            }

            foreach (var group in groups)
            {
                ElementMaxLengthValidation.Validate(group, 64, $"{name} Group", dicomElement.ValueRepresentation);

                if (group.ToCharArray().Any(IsControlExceptESC))
                {
                    throw new DicomElementValidationException(ElementValidationErrorCode.ValueContainsInvalidCharacters, name, DicomVR.PN, DicomCoreResource.ValueContainsInvalidCharacter, value);
                }
            }

            var groupcomponents = groups.Select(group => group.Split('^').Length);
            if (groupcomponents.Any(l => l > 5))
            {
                throw new DicomElementValidationException(ElementValidationErrorCode.PatientNameHasTooManyComponents, name, DicomVR.PN, DicomCoreResource.ValueExceedsAllowedComponents, value);
            }
        }
    }
}
