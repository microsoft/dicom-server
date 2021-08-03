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
    internal class DicomElementRequiredLengthValidation : DicomElementValidation
    {
        public int RequiredLength { get; }

        public DicomElementRequiredLengthValidation(int requiredLength)
        {
            RequiredLength = requiredLength;
        }

        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);
            DicomVR vr = dicomElement.ValueRepresentation;
            if (ValidationLimits.CanGetAsString(vr))
            {
                ValidateStringLength(vr, dicomElement.Tag.GetFriendlyName(), dicomElement.Get<string>());
            }
            else
            {
                ValidateByteBufferLength(vr, dicomElement.Tag.GetFriendlyName(), dicomElement.Buffer);
            }
        }
        private void ValidateByteBufferLength(DicomVR dicomVR, string name, IByteBuffer value)
        {
            if (value?.Size != RequiredLength)
            {
                throw new DicomElementValidationException(
                    ValidationErrorCode.ValueLengthIsNotRequiredLength,
                    name,
                    dicomVR,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthIsNotRequiredLength, RequiredLength));
            }
        }

        private void ValidateStringLength(DicomVR dicomVR, string name, string value)
        {
            if (value?.Length != RequiredLength)
            {
                throw new DicomElementValidationException(
                    ValidationErrorCode.ValueLengthIsNotRequiredLength,
                    name,
                    dicomVR,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthIsNotRequiredLength, RequiredLength),
                    value);
            }
        }
    }
}
