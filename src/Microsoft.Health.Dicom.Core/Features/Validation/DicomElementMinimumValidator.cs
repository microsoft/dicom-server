// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dicom;
using Dicom.IO.Buffer;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {
        private readonly Dictionary<DicomVR, Action<string, string>> _stringValidators = new Dictionary<DicomVR, Action<string, string>>();
        private readonly Dictionary<DicomVR, Action<IByteBuffer, string>> _byteBufferValidators = new Dictionary<DicomVR, Action<IByteBuffer, string>>();

        public DicomElementMinimumValidator()
        {
            _stringValidators.Add(DicomVR.AE, DicomElementMinimumValidation.ValidateAE);
            _stringValidators.Add(DicomVR.AS, DicomElementMinimumValidation.ValidateAS);
            _stringValidators.Add(DicomVR.CS, DicomElementMinimumValidation.ValidateCS);
            _stringValidators.Add(DicomVR.DA, DicomElementMinimumValidation.ValidateDA);
            _stringValidators.Add(DicomVR.DS, DicomElementMinimumValidation.ValidateDS);
            _stringValidators.Add(DicomVR.DT, DicomElementMinimumValidation.ValidateDT);
            _stringValidators.Add(DicomVR.IS, DicomElementMinimumValidation.ValidateIS);
            _stringValidators.Add(DicomVR.LO, DicomElementMinimumValidation.ValidateLO);
            _stringValidators.Add(DicomVR.PN, DicomElementMinimumValidation.ValidatePN);
            _stringValidators.Add(DicomVR.SH, DicomElementMinimumValidation.ValidateSH);
            _stringValidators.Add(DicomVR.TM, DicomElementMinimumValidation.ValidateTM);
            _stringValidators.Add(DicomVR.UI, DicomElementMinimumValidation.ValidateUI);

            _byteBufferValidators.Add(DicomVR.AT, DicomElementMinimumValidation.ValidateAT);
            _byteBufferValidators.Add(DicomVR.FL, DicomElementMinimumValidation.ValidateFL);
            _byteBufferValidators.Add(DicomVR.FD, DicomElementMinimumValidation.ValidateFD);
            _byteBufferValidators.Add(DicomVR.SL, DicomElementMinimumValidation.ValidateSL);
            _byteBufferValidators.Add(DicomVR.SS, DicomElementMinimumValidation.ValidateSS);
            _byteBufferValidators.Add(DicomVR.UL, DicomElementMinimumValidation.ValidateUL);
            _byteBufferValidators.Add(DicomVR.US, DicomElementMinimumValidation.ValidateUS);
        }

        // only works for single value dicom element
        public void Validate(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            DicomVR dicomVR = element.ValueRepresentation;

            if (dicomVR == null)
            {
                Debug.Fail("Dicom VR type should not be null");
            }

            if (_stringValidators.TryGetValue(dicomVR, out Action<string, string> stringValidator))
            {
                stringValidator(element.Get<string>(), GetName(element.Tag));
            }
            else if (_byteBufferValidators.TryGetValue(dicomVR, out Action<IByteBuffer, string> byteBufferValidator))
            {
                byteBufferValidator(element.Buffer, GetName(element.Tag));
            }
            else
            {
                Debug.Fail($"Missing validation action for for VR :{dicomVR.Code}, add a new validation and register in the constructor.");
            }
        }

        private static string GetName(DicomTag dicomTag) => dicomTag.IsPrivate ? dicomTag.GetPath() : dicomTag.DictionaryEntry.Keyword;
    }
}
