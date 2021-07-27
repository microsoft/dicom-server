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
    internal abstract class DicomElementValidation : IValidationRule
    {
        public virtual void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (ValidationLimits.SupportedVRs.TryGetValue(vr, out _))
            {
                // only works for single value dicom element
                if (dicomElement.Count != 1)
                {
                    throw new DicomElementValidationException(ValidationErrorCode.MultipleElementDetected, dicomElement.Tag.GetFriendlyName(), vr, "Multiple elements are detected");
                }
            }
            else
            {
                Debug.Fail($"Validating VR {vr.Code} is not supported.");
            }
        }
    }
}
