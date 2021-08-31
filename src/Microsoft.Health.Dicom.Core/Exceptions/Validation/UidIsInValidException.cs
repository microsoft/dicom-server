// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class UidIsInValidException : ElementValidationException
    {
        public UidIsInValidException(string name, string value) :
            base(name, DicomVR.UI, value, Features.Validation.ValidationErrorCode.UidIsInvalid, DicomCoreResource.ErrorMessageUidIsInvalid)
        {
        }
    }
}
