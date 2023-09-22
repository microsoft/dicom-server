// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

public class ElementMinimumValidator : IElementMinimumValidator
{
    private static readonly IReadOnlyDictionary<DicomVR, IElementValidation> Validations = new Dictionary<DicomVR, IElementValidation>
    {
        { DicomVR.AE, new ElementMaxLengthValidation(16) },
        { DicomVR.AS, new ElementRequiredLengthValidation(4) },
        { DicomVR.CS, new ElementMaxLengthValidation(16) },
        { DicomVR.DA, new DateValidation() },
        { DicomVR.DT, new EncodedStringElementValidation() },
        { DicomVR.FD, new ElementRequiredLengthValidation(8) },
        { DicomVR.FL, new ElementRequiredLengthValidation(4) },
        { DicomVR.IS, new EncodedStringElementValidation() },
        { DicomVR.LO, new LongStringValidation() },
        { DicomVR.PN, new PersonNameValidation() },
        { DicomVR.SH, new ElementMaxLengthValidation(16) },
        { DicomVR.SL, new ElementRequiredLengthValidation(4) },
        { DicomVR.SS, new ElementRequiredLengthValidation(2) },
        { DicomVR.TM, new EncodedStringElementValidation() },
        { DicomVR.UI, new UidValidation() },
        { DicomVR.UL, new ElementRequiredLengthValidation(4) },
        { DicomVR.US, new ElementRequiredLengthValidation(2) },
    };

    public void Validate(DicomElement dicomElement, bool withLeniency = false)
    {
        EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
        DicomVR vr = dicomElement.ValueRepresentation;
        if (vr == null)
        {
            Debug.Fail("Dicom VR type should not be null");
        }
        if (Validations.TryGetValue(vr, out IElementValidation validationRule))
        {
            validationRule.Validate(dicomElement, withLeniency);
        }
        else
        {
            Debug.Fail($"Validating VR {vr?.Code} is not supported.");
        }
    }
}
