// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class UnexpectedLengthException : ElementValidationException
    {
        public UnexpectedLengthException(string name, DicomVR vr, string value, int expectedLength)
            : base(name, vr, value, ValidationErrorCode.UnexpectedLength, string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, expectedLength))
        {
            ExpectedLength = expectedLength;
        }

        public UnexpectedLengthException(string name, DicomVR vr, int expectedLength) : base(name, vr, ValidationErrorCode.UnexpectedLength, string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedLength, expectedLength))
        {
            ExpectedLength = expectedLength;
        }

        public int ExpectedLength { get; }
    }
}
