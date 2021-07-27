// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class DicomElementMaxLengthValidation : DicomElementValidation
    {
        public DicomElementMaxLengthValidation(int maxLength)
        {
            MaxLength = maxLength;
        }

        public int MaxLength { get; }


        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            string value = dicomElement.Get<string>();
            if (value?.Length > MaxLength)
            {
                throw new DicomStringElementValidationException(
                    ValidationErrorCode.ValueIsTooLong,
                    dicomElement.Tag.GetFriendlyName(),
                    value,
                    dicomElement.ValueRepresentation,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthAboveMaxLength, MaxLength));
            }
        }
    }
}
