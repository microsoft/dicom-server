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
    public partial class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {

        private static readonly IReadOnlyDictionary<DicomVR, IValidationRule> Validations = new Dictionary<DicomVR, IValidationRule>
        {
            { DicomVR.AE, new MaxLengthValidation(16) },
            { DicomVR.AS, new RequiredLengthValidation(4) },
            { DicomVR.CS, new MaxLengthValidation(16) },
            { DicomVR.DA, new DateValidation() },
            { DicomVR.DS, new MaxLengthValidation(16) },
            { DicomVR.FL, new RequiredLengthValidation(4) },
            { DicomVR.FD, new RequiredLengthValidation(8) },
            { DicomVR.IS, new MaxLengthValidation(12) },
            { DicomVR.LO, new LongStringValidation() },
            { DicomVR.PN, new PatientNameValidation() },
            { DicomVR.SH, new MaxLengthValidation(16) },
            { DicomVR.SL, new RequiredLengthValidation(4) },
            { DicomVR.SS, new RequiredLengthValidation(2) },
            { DicomVR.UI, new UidValidation() },
            { DicomVR.UL, new RequiredLengthValidation(4) },
            { DicomVR.US, new RequiredLengthValidation(2) },
        };

        public void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (vr == null)
            {
                Debug.Fail("Dicom VR type should not be null");
            }
            if (Validations.TryGetValue(vr, out IValidationRule validationRule))
            {
                validationRule.Validate(dicomElement);
            }
            else
            {
                Debug.Fail($"Validating VR {vr.Code} is not supported.");
            }
        }

    }
}
