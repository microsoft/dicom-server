// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation.Errors;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class PersonNameValidation : ElementValidation
    {
        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            string value = dicomElement.Get<string>();
            string name = dicomElement.Tag.GetFriendlyName();
            DicomVR vr = dicomElement.ValueRepresentation;
            if (string.IsNullOrEmpty(value))
            {
                // empty values allowed
                return;
            }

            var groups = value.Split('=');
            if (groups.Length > 3)
            {
                throw new DicomElementValidationException(new PersonNameExceedMaxGroupsError(name, value));
            }

            foreach (var group in groups)
            {
                try
                {
                    ElementMaxLengthValidation.Validate(group, 64, name, dicomElement.ValueRepresentation);
                }
                catch (DicomElementValidationException ex)
                {
                    // Reprocess the exception to make more meaningful message
                    if (ex.Error is ExceedMaxLengthError)
                    {
                        throw new DicomElementValidationException(new PersonNameGroupExceedMaxLengthError(name, value));
                    }
                }

                if (ContainsControlExceptEsc(group))
                {
                    throw new DicomElementValidationException(new InvalidCharactersError(name, vr, value));
                }
            }

            var groupcomponents = groups.Select(group => group.Split('^').Length);
            if (groupcomponents.Any(l => l > 5))
            {
                throw new DicomElementValidationException(new PersonNameExceedMaxComponentsError(name, value));
            }
        }
    }
}
