// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class UidIsInValidError : ElementValidationError
    {
        public UidIsInValidError(string name, string value) : base(name, DicomVR.UI, value)
        {

        }
        public override ValidationErrorCode ErrorCode => ValidationErrorCode.UidIsInvalid;

        public override string GetBriefMessage()
        {
            throw new NotImplementedException();
        }

        protected override string GetErrorMessage()
        {
            return DicomCoreResource.InvalidDicomIdentifier;
        }
    }
}
