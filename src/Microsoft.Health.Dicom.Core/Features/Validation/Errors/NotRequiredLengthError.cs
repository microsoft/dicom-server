// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class UnexpectedLengthError
        : ElementValidationError
    {
        public UnexpectedLengthError(string name, DicomVR vr, string value, int expectedLength) : base(name, vr, value)
        {
            ExpectedLength = expectedLength;
        }

        public UnexpectedLengthError(string name, DicomVR vr, int expectedLength) : base(name, vr)
        {
            ExpectedLength = expectedLength;
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.UnexpectedLength;

        public int ExpectedLength { get; }

        protected override string GetInnerMessage()
        {
            return string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, ExpectedLength);
        }
    }
}
