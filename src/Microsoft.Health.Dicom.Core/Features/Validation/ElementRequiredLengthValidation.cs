// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation.Errors;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class ElementRequiredLengthValidation : ElementValidation
    {
        private static readonly HashSet<DicomVR> StringVrs = new HashSet<DicomVR>()
        {
           DicomVR.AE,
           DicomVR.AS,
           DicomVR.CS,
           DicomVR.DA,
           DicomVR.DS,
           DicomVR.IS,
           DicomVR.LO,
           DicomVR.PN,
           DicomVR.SH,
           DicomVR.UI,
        };

        public int RequiredLength { get; }

        public ElementRequiredLengthValidation(int requiredLength)
        {
            Debug.Assert(requiredLength >= 0, "Required Length should be none-negative");
            RequiredLength = requiredLength;
        }

        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);
            DicomVR vr = dicomElement.ValueRepresentation;
            if (TryGetAsString(dicomElement, out string value))
            {
                ValidateStringLength(vr, dicomElement.Tag.GetFriendlyName(), value);
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
                throw new DicomElementValidationException(new NotRequiredLengthError(name, dicomVR, RequiredLength));
            }
        }

        private static bool TryGetAsString(DicomElement dicomElement, out string value)
        {
            value = string.Empty;
            if (StringVrs.Contains(dicomElement.ValueRepresentation))
            {
                value = dicomElement.Get<string>();
                return true;
            }

            return false;
        }

        private void ValidateStringLength(DicomVR dicomVR, string name, string value)
        {
            value = value ?? "";
            if (value.Length != RequiredLength)
            {
                throw new DicomElementValidationException(new NotRequiredLengthError(name, dicomVR, value, RequiredLength));
            }
        }
    }
}
