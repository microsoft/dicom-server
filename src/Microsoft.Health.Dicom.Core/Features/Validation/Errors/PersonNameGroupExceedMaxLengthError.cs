// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class PersonNameGroupExceedMaxLengthError : ElementValidationError
    {
        public PersonNameGroupExceedMaxLengthError(string name, string value) : base(name, DicomVR.PN, value)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.PersonNameGroupExceedMaxLength;

        protected override string GetInnerMessage() => DicomCoreResource.ErrorMessagePersonNameGroupExceedMaxLength;
    }
}
