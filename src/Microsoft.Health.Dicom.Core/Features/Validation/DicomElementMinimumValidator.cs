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
        private readonly Dictionary<DicomVR, Action<string, string>> _stringValidator = new Dictionary<DicomVR, Action<string, string>>();
        private readonly Dictionary<DicomVR, Action<IByteBuffer, string>> _byteBufferValidator = new Dictionary<DicomVR, Action<IByteBuffer, string>>();

        public DicomElementMinimumValidator()
        {
            _stringValidator.Add(DicomVR.AE, DicomElementMinimumValidation.ValidateAE);
            _stringValidator.Add(DicomVR.AS, DicomElementMinimumValidation.ValidateAS);
            _stringValidator.Add(DicomVR.CS, DicomElementMinimumValidation.ValidateCS);
            _stringValidator.Add(DicomVR.DA, DicomElementMinimumValidation.ValidateDA);
            _stringValidator.Add(DicomVR.DS, DicomElementMinimumValidation.ValidateDS);
            _stringValidator.Add(DicomVR.DT, DicomElementMinimumValidation.ValidateDT);
            _stringValidator.Add(DicomVR.IS, DicomElementMinimumValidation.ValidateIS);
            _stringValidator.Add(DicomVR.LO, DicomElementMinimumValidation.ValidateLO);
            _stringValidator.Add(DicomVR.PN, DicomElementMinimumValidation.ValidatePN);
            _stringValidator.Add(DicomVR.SH, DicomElementMinimumValidation.ValidateSH);
            _stringValidator.Add(DicomVR.TM, DicomElementMinimumValidation.ValidateTM);
            _stringValidator.Add(DicomVR.UI, DicomElementMinimumValidation.ValidateUI);

            _byteBufferValidator.Add(DicomVR.AT, DicomElementMinimumValidation.ValidateAT);
            _byteBufferValidator.Add(DicomVR.FL, DicomElementMinimumValidation.ValidateFL);
            _byteBufferValidator.Add(DicomVR.FD, DicomElementMinimumValidation.ValidateFD);
            _byteBufferValidator.Add(DicomVR.SL, DicomElementMinimumValidation.ValidateSL);
            _byteBufferValidator.Add(DicomVR.SS, DicomElementMinimumValidation.ValidateSS);
            _byteBufferValidator.Add(DicomVR.UL, DicomElementMinimumValidation.ValidateUL);
            _byteBufferValidator.Add(DicomVR.US, DicomElementMinimumValidation.ValidateUS);
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

            if (_stringValidator.TryGetValue(dicomVR, out Action<string, string> stringValidator))
            {
                stringValidator(element.Get<string>(), GetName(element.Tag));
            }
            else if (_byteBufferValidator.TryGetValue(dicomVR, out Action<IByteBuffer, string> byteBufferValidator))
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
