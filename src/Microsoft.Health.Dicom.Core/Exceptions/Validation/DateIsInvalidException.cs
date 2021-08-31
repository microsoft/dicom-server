// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class DateIsInvalidException : ElementValidationException
    {
        public DateIsInvalidException(string name, string value) :
            base(name, DicomVR.DA, value, ValidationErrorCode.DateIsInvalid, DicomCoreResource.ErrorMessageDateIsInvalid)
        {
        }
    }
}
