// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {
        private readonly Dictionary<DicomVR, Action<string, string>> _minValidators = new Dictionary<DicomVR, Action<string, string>>();

        public DicomElementMinimumValidator()
        {
            _minValidators.Add(DicomVR.CS, DicomElementMinimumValidation.ValidateCS);
            _minValidators.Add(DicomVR.LO, DicomElementMinimumValidation.ValidateLO);
            _minValidators.Add(DicomVR.SH, DicomElementMinimumValidation.ValidateSH);
            _minValidators.Add(DicomVR.PN, DicomElementMinimumValidation.ValidatePN);
            _minValidators.Add(DicomVR.DA, DicomElementMinimumValidation.ValidateDA);
            _minValidators.Add(DicomVR.UI, DicomElementMinimumValidation.ValidateUI);
        }

        // only works for single value dicom element
        public void Validate(DicomTag dicomTag, string value)
        {
            DicomVR dicomVR = dicomTag.DictionaryEntry.ValueRepresentations.FirstOrDefault();

            if (dicomVR == null)
            {
                Debug.Fail("Dicom VR type should not be null");
            }

            if (_minValidators.TryGetValue(dicomVR, out Action<string, string> validator))
            {
                validator(value, dicomTag.DictionaryEntry.Keyword);
            }
            else
            {
                Debug.Fail($"Missing validation action for for VR :{dicomVR.Code}, add a new validation and register in the constructor.");
            }
        }
    }
}
