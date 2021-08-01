// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {

        private static readonly IReadOnlyDictionary<DicomVR, IDicomElementValidation> Validations = new Dictionary<DicomVR, IDicomElementValidation>
        {
            { DicomVR.AE, new DicomElementMaxLengthValidation(16) },
            { DicomVR.AS, new DicomElementRequiredLengthValidation(4) },
            { DicomVR.CS, new DicomElementMaxLengthValidation(16) },
            { DicomVR.DA, new DicomDateValidation() },
            { DicomVR.DS, new DicomElementMaxLengthValidation(16) },
            { DicomVR.FL, new DicomElementRequiredLengthValidation(4) },
            { DicomVR.FD, new DicomElementRequiredLengthValidation(8) },
            { DicomVR.IS, new DicomElementMaxLengthValidation(12) },
            { DicomVR.LO, new DicomLongStringValidation() },
            { DicomVR.PN, new DicomPersonNameValidation() },
            { DicomVR.SH, new DicomElementMaxLengthValidation(16) },
            { DicomVR.SL, new DicomElementRequiredLengthValidation(4) },
            { DicomVR.SS, new DicomElementRequiredLengthValidation(2) },
            { DicomVR.UI, new DicomUidValidation() },
            { DicomVR.UL, new DicomElementRequiredLengthValidation(4) },
            { DicomVR.US, new DicomElementRequiredLengthValidation(2) },
        };

        public void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (vr == null)
            {
                Debug.Fail("Dicom VR type should not be null");
            }
            if (Validations.TryGetValue(vr, out IDicomElementValidation validationRule))
            {
                validationRule.Validate(dicomElement);
            }
            else
            {
                Debug.Fail($"Validating VR {vr?.Code} is not supported.");
            }
        }

    }
}
