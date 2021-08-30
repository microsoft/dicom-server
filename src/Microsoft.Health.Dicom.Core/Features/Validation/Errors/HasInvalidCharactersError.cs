// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class HasInvalidCharactersError
        : ElementValidationError
    {
        public HasInvalidCharactersError(string name, DicomVR vr, string value) : base(name, vr, value)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.HasInvalidCharacters;

        public override string GetBriefMessage() => DicomCoreResource.HasInvalidCharactersError;

        protected override string GetErrorMessage() => DicomCoreResource.HasInvalidCharactersError;


    }
}
