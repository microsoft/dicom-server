// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class UnexpectedVRError : ElementValidationError
    {

        public UnexpectedVRError(string name, DicomVR vr, DicomVR expectedVR) : base(name, vr)
        {
            ExpectedVR = EnsureArg.IsNotNull(expectedVR, nameof(expectedVR));
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.UnexpectedVR;

        public DicomVR ExpectedVR { get; }

        protected override string GetInnerMessage()
        {
            return string.Format(
                               CultureInfo.InvariantCulture,
                               DicomCoreResource.ErrorMessageUnexpectedVR,
                               Name,
                               ExpectedVR,
                               VR);
        }
    }
}
