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
    public class ElementMinimumValidator : IElementMinimumValidator
    {

        private static readonly IReadOnlyDictionary<DicomVR, IElementValidation> Validations = new Dictionary<DicomVR, IElementValidation>
        {
            { DicomVR.AE, new ElementMaxLengthValidation(16) },
            { DicomVR.AS, new ElementRequiredLengthValidation(4) },
            { DicomVR.CS, new ElementMaxLengthValidation(16) },
            { DicomVR.DA, new DateValidation() },
            { DicomVR.DS, new ElementMaxLengthValidation(16) },
            { DicomVR.DT, new DateTimeValidation() },
            { DicomVR.FL, new ElementRequiredLengthValidation(4) },
            { DicomVR.FD, new ElementRequiredLengthValidation(8) },
            { DicomVR.IS, new ElementMaxLengthValidation(12) },
            { DicomVR.LO, new LongStringValidation() },
            { DicomVR.PN, new PersonNameValidation() },
            { DicomVR.SH, new ElementMaxLengthValidation(16) },
            { DicomVR.SL, new ElementRequiredLengthValidation(4) },
            { DicomVR.SS, new ElementRequiredLengthValidation(2) },
            { DicomVR.TM, new TimeValidation() },
            { DicomVR.UI, new UidValidation() },
            { DicomVR.UL, new ElementRequiredLengthValidation(4) },
            { DicomVR.US, new ElementRequiredLengthValidation(2) },
        };

        public void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (vr == null)
            {
                Debug.Fail("Dicom VR type should not be null");
            }
            if (Validations.TryGetValue(vr, out IElementValidation validationRule))
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
