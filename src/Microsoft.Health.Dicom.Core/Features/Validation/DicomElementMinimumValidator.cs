// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {
        private readonly Dictionary<DicomVR, Action<DicomElement>> _minValidators = new Dictionary<DicomVR, Action<DicomElement>>();

        public DicomElementMinimumValidator()
        {
            _minValidators.Add(DicomVR.AE, DicomElementMinimumValidation.ValidateAE);
            _minValidators.Add(DicomVR.AS, DicomElementMinimumValidation.ValidateAS);
            _minValidators.Add(DicomVR.AT, DicomElementMinimumValidation.ValidateAT);
            _minValidators.Add(DicomVR.CS, DicomElementMinimumValidation.ValidateCS);
            _minValidators.Add(DicomVR.DA, DicomElementMinimumValidation.ValidateDA);
            _minValidators.Add(DicomVR.DS, DicomElementMinimumValidation.ValidateDS);
            _minValidators.Add(DicomVR.DT, DicomElementMinimumValidation.ValidateDT);
            _minValidators.Add(DicomVR.FL, DicomElementMinimumValidation.ValidateFL);
            _minValidators.Add(DicomVR.FD, DicomElementMinimumValidation.ValidateFD);
            _minValidators.Add(DicomVR.IS, DicomElementMinimumValidation.ValidateIS);
            _minValidators.Add(DicomVR.LO, DicomElementMinimumValidation.ValidateLO);
            _minValidators.Add(DicomVR.PN, DicomElementMinimumValidation.ValidatePN);
            _minValidators.Add(DicomVR.SH, DicomElementMinimumValidation.ValidateSH);
            _minValidators.Add(DicomVR.SL, DicomElementMinimumValidation.ValidateSL);
            _minValidators.Add(DicomVR.SS, DicomElementMinimumValidation.ValidateSS);
            _minValidators.Add(DicomVR.TM, DicomElementMinimumValidation.ValidateTM);
            _minValidators.Add(DicomVR.UI, DicomElementMinimumValidation.ValidateUI);
            _minValidators.Add(DicomVR.UL, DicomElementMinimumValidation.ValidateUL);
            _minValidators.Add(DicomVR.US, DicomElementMinimumValidation.ValidateUS);
        }

        // only works for single value dicom element
        public void Validate(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));

            if (_minValidators.TryGetValue(element.ValueRepresentation, out Action<DicomElement> validator))
            {
                validator(element);
            }
            else
            {
                Debug.Fail($"Missing validation action for for VR :{element.ValueRepresentation.Code}, add a new validation and register in the constructor.");
            }
        }
    }
}
