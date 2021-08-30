// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Validation.Errors
{
    public class MultiValuesError : ElementValidationError
    {
        public MultiValuesError(string name, DicomVR vr) : base(name, vr)
        {
        }

        public override ValidationErrorCode ErrorCode => ValidationErrorCode.MultiValues;

        protected override string GetInnerMessage()
        {
            return DicomCoreResource.ErrorMessageMultiValues;
        }

    }
}
