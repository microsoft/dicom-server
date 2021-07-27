// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public partial class DicomElementMinimumValidator : IDicomElementMinimumValidator
    {

        // only works for single value dicom element
        public void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (vr == null)
            {
                Debug.Fail("Dicom VR type should not be null");
            }
            if (SupportedVRs.TryGetValue(vr, out DicomVRType vrType))
            {

                // Validate MaxLength
                if (MaxLengthValidations.ContainsKey(vr))
                {
                    ValidateLengthNotExceed(vr, dicomElement.Tag.GetFriendlyName(), dicomElement.Get<string>());
                }

                // Validate Required Length
                if (RequiredLengthValidations.ContainsKey(vr))
                {
                    if (vrType == DicomVRType.Binary)
                    {
                        ValidateByteBufferLengthIsRequired(vr, dicomElement.Tag.ToString(), dicomElement.Buffer);
                    }
                    else
                    {
                        ValidateStringLengthIsRequired(vr, dicomElement.Tag.ToString(), dicomElement.Get<string>());
                    }
                }

                // Other validation
                if (OtherValidations.ContainsKey(vr))
                {
                    OtherValidations[vr].Invoke(dicomElement);
                }
            }
            else
            {
                Debug.Fail($"Validating VR {vr.Code} is not supported.");
            }
        }

    }
}
