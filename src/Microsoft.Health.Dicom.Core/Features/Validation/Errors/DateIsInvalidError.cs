// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class DateIsInvalidError : ElementValidationError
    {
        public DateIsInvalidError(string name, DicomVR vr, string value) : base(name, vr, value)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.DateIsInvalid;

        protected override string GetInnerMessage() => DicomCoreResource.ErrorMessageDateIsInvalid;
    }
}
