// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class PersonNameExceedMaxComponentsError : ElementValidationError
    {

        public PersonNameExceedMaxComponentsError(string name, DicomVR vr, string value) : base(name, vr, value)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.PersonNameExceedMaxComponents;

        public override string GetBriefMessage() => DicomCoreResource.PersonNameExceedMaxComponentsError;

        protected override string GetErrorMessage() => DicomCoreResource.PersonNameExceedMaxComponentsError;
    }
}
