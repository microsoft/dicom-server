// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class PersonNameGroupHasInvalidCharactersError : ElementValidationError
    {

        public PersonNameGroupHasInvalidCharactersError(string name, DicomVR vr, string value) : base(name, vr, value)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.PersonNameGroupHasInvalidCharacters;

        public override string GetBriefMessage()
        {
            throw new System.NotImplementedException();
        }

        protected override string GetErrorMessage()
        {
            throw new System.NotImplementedException();
        }
    }
}
