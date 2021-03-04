// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {
        private readonly Dictionary<DicomVR, Action<DicomElement>> _minValidators = new Dictionary<DicomVR, Action<DicomElement>>();

        public DicomElementMinimumValidator()
        {
            _minValidators.Add(DicomVR.CS, DicomElementMinimumValidation.ValidateCS);
            _minValidators.Add(DicomVR.DA, DicomElementMinimumValidation.ValidateDA);
            _minValidators.Add(DicomVR.LO, DicomElementMinimumValidation.ValidateLO);
            _minValidators.Add(DicomVR.PN, DicomElementMinimumValidation.ValidatePN);
            _minValidators.Add(DicomVR.SH, DicomElementMinimumValidation.ValidateSH);
            _minValidators.Add(DicomVR.UI, DicomElementMinimumValidation.ValidateUI);
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
                // Use default validator provided by Fo-dicom, if we see problems in the feature, could create custom ones.
                DicomElementMinimumValidation.DefaultValidate(element);
            }
        }
    }
}
