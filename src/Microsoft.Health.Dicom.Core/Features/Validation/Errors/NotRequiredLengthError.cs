// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class NotRequiredLengthError : ElementValidationError
    {
        public NotRequiredLengthError(string name, DicomVR vr, string value, int requiredLength) : base(name, vr, value)
        {
            RequiredLength = requiredLength;
        }

        public NotRequiredLengthError(string name, DicomVR vr, int requiredLength) : base(name, vr)
        {
            RequiredLength = requiredLength;
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.NotRequiredLength;

        public int RequiredLength { get; }

        protected override string GetInnerMessage()
        {
            return string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageNotRequiredLength, RequiredLength);
        }
    }
}
