// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class NotRequiredVRError : ElementValidationError
    {

        public NotRequiredVRError(string name, DicomVR vr, DicomVR requiredVR) : base(name, vr)
        {
            RequiredVR = EnsureArg.IsNotNull(requiredVR, nameof(requiredVR));
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.UnexpectedVR;

        public DicomVR RequiredVR { get; }

        protected override string GetInnerMessage()
        {
            return string.Format(
                               CultureInfo.InvariantCulture,
                               DicomCoreResource.ErrorMessageNotRequiredVR,
                               Name,
                               RequiredVR,
                               VR);
        }
    }
}
