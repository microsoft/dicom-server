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
        private readonly int _maxlength;

        public ExceedMaxLengthError(string name, DicomVR vr, string value, int maxlength) : base(name, vr, value)
        {
            _maxlength = maxlength;
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.ExceedMaxLength;

        protected override string GetInnerMessage()
        {
            return string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageExceedMaxLength, _maxlength);
        }
    }
}
