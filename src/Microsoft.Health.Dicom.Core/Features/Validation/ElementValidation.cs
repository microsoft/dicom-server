// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class ElementValidation : IElementValidation
    {
        public virtual void Validate(DicomElement dicomElement)
        {
            EnsureArg.IsNotNull(dicomElement, nameof(dicomElement));
            DicomVR vr = dicomElement.ValueRepresentation;
            if (ExtendedQueryTagEntryValidator.SupportedVRCodes.Contains(vr.Code))
            {
                // only works for single value dicom element ( Since we accept empty/null value, Count = 0 is accepted).
                if (dicomElement.Count > 1)
                {
                    throw ElementValidationExceptionFactory.CreateMultiValuesException(dicomElement.Tag.GetFriendlyName(), vr);
                }
            }
            else
            {
                Debug.Fail($"Validating VR {vr.Code} is not supported.");
            }
        }

        protected static bool ContainsControlExceptEsc(string text)
        {
            return text == null ? false : text.Any(c => char.IsControl(c) && (c != '\u001b'));
        }

    }
}
