// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class PersonNameGroupExceedMaxLengthException : ElementValidationException
    {
        public PersonNameGroupExceedMaxLengthException(string name, string value) :
            base(name, DicomVR.PN, value, ValidationErrorCode.PersonNameGroupExceedMaxLength, DicomCoreResource.ErrorMessagePersonNameGroupExceedMaxLength)
        {
        }
    }
}
