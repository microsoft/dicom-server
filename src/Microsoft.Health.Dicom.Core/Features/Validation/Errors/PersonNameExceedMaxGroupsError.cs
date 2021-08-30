// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class PersonNameExceedMaxGroupsError : ElementValidationError
    {
        public PersonNameExceedMaxGroupsError(string name, string value) : base(name, DicomVR.PN, value)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.PersonNameExceedMaxGroups;

        protected override string GetInnerMessage() => DicomCoreResource.ErrorMessagePersonNameExceedMaxGroups;
    }
}
