// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class ExceedMaxLengthError : ElementValidationError
    {
        public ExceedMaxLengthError(string name, DicomVR vr, string value, int maxlength) : base(name, vr, value)
        {
            Maxlength = maxlength;
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.ExceedMaxLength;

        public int Maxlength { get; }

        protected override string GetInnerMessage()
        {
            return string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageExceedMaxLength, Maxlength);
        }
    }
}
