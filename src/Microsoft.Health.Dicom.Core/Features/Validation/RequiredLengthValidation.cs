// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class RequiredLengthValidation : DicomElementValidation
    {
        public int RequiredLength { get; }

        public RequiredLengthValidation(int requiredLength)
        {
            RequiredLength = requiredLength;
        }


        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);
            DicomVR vr = dicomElement.ValueRepresentation;
            if (ValidationLimits.SupportedVRs.TryGetValue(vr, out DicomVRType vrType))
            {
                if (vrType == DicomVRType.Binary)
                {
                    ValidateByteBufferLengthIsRequired(vr, dicomElement.Tag.GetFriendlyName(), dicomElement.Buffer);
                }
                else
                {
                    ValidateStringLengthIsRequired(vr, dicomElement.Tag.GetFriendlyName(), dicomElement.Get<string>());
                }
            }
        }
        private void ValidateByteBufferLengthIsRequired(DicomVR dicomVR, string name, IByteBuffer value)
        {
            if (value?.Size != RequiredLength)
            {
                throw new DicomElementValidationException(
                    ValidationErrorCode.ValueIsNotRequiredLength,
                    name,
                    dicomVR,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthIsNotRequiredLength, RequiredLength));
            }
        }

        private void ValidateStringLengthIsRequired(DicomVR dicomVR, string name, string value)
        {
            if (value?.Length != RequiredLength)
            {
                throw new DicomStringElementValidationException(
                    ValidationErrorCode.ValueIsNotRequiredLength,
                    name,
                    value,
                    dicomVR,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthIsNotRequiredLength, RequiredLength));
            }
        }
    }
}
