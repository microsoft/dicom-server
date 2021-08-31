// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class UnexpectedVRException : ElementValidationException
    {
        public UnexpectedVRException(string name, DicomVR vr, DicomVR expectedVR) :
            base(name, vr, ValidationErrorCode.UnexpectedVR,
            string.Format(
                               CultureInfo.InvariantCulture,
                               DicomCoreResource.ErrorMessageUnexpectedVR,
                               name,
                               EnsureArg.IsNotNull(expectedVR, nameof(expectedVR)),
                               vr))
        {
            ExpectedVR = EnsureArg.IsNotNull(expectedVR, nameof(expectedVR));
        }

        public DicomVR ExpectedVR { get; }
    }
}
