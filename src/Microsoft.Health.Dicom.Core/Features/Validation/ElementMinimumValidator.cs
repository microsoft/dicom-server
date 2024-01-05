// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation;

public class ElementMinimumValidator : IElementMinimumValidator
{
    private static readonly ImmutableDictionary<DicomVR, IElementValidation> Validations = ImmutableDictionary.CreateRange(
        new KeyValuePair<DicomVR, IElementValidation>[]
        {
            new(DicomVR.AE, new ElementMaxLengthValidation(16)),
            new(DicomVR.AS, new ElementRequiredLengthValidation(4)),
            new(DicomVR.CS, new ElementMaxLengthValidation(16)),
            new(DicomVR.DA, new DateValidation()),
            new(DicomVR.DT, new EncodedStringElementValidation()),
            new(DicomVR.FD, new ElementRequiredLengthValidation(8)),
            new(DicomVR.FL, new ElementRequiredLengthValidation(4)),
            new(DicomVR.IS, new EncodedStringElementValidation()),
            new(DicomVR.LO, new LongStringValidation()),
            new(DicomVR.PN, new PersonNameValidation()),
            new(DicomVR.SH, new ElementMaxLengthValidation(16)),
            new(DicomVR.SL, new ElementRequiredLengthValidation(4)),
            new(DicomVR.SS, new ElementRequiredLengthValidation(2)),
            new(DicomVR.TM, new EncodedStringElementValidation()),
            new(DicomVR.UI, new UidValidation()),
            new(DicomVR.UL, new ElementRequiredLengthValidation(4)),
            new(DicomVR.US, new ElementRequiredLengthValidation(2)),
        });

    public void Validate(DicomElement dicomElement, ValidationLevel validationLevel = ValidationLevel.Strict)
    {
        EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
        DicomVR vr = dicomElement.ValueRepresentation;
        if (vr == null)
        {
            Debug.Fail("Dicom VR type should not be null");
        }
        if (Validations.TryGetValue(vr, out IElementValidation validationRule))
        {
            validationRule.Validate(dicomElement, validationLevel);
        }
        else
        {
            Debug.Fail($"Validating VR {vr?.Code} is not supported.");
        }
    }
}
