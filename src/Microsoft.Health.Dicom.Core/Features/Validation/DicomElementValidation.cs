// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class DicomElementValidation : IValidation
    {
        public virtual void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (ValidationLimits.SupportedVRs.TryGetValue(vr, out _))
            {
                // only works for single value dicom element
                // Since we accept empty/null value, hence Count = 0 is accepted.
                if (dicomElement.Count > 1)
                {
                    throw new DicomElementValidationException(ValidationErrorCode.ValueHasMultipleItems, dicomElement.Tag.GetFriendlyName(), vr, "Multiple elements are detected");
                }
            }
            else
            {
                Debug.Fail($"Validating VR {vr.Code} is not supported.");
            }
        }

        protected static bool IsControlExceptESC(char c)
        => char.IsControl(c) && (c != '\u001b');
    }
}
