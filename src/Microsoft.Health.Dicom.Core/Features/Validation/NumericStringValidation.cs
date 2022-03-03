// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class NumericStringValidation : ElementValidation
    {
        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            DicomVR vr = dicomElement.ValueRepresentation;
            if (vr != DicomVR.DS && vr != DicomVR.IS)
            {
                throw new ArgumentOutOfRangeException(nameof(dicomElement));
            }

            string value = dicomElement.Get<string>();
            if (!string.IsNullOrEmpty(value))
            {
                if (vr == DicomVR.DS)
                {
                    DicomValidation.ValidateDS(value);
                }
                else
                {
                    DicomValidation.ValidateIS(value);
                }
            }
        }
    }
}
