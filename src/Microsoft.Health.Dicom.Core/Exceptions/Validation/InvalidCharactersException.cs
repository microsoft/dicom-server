// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class InvalidCharactersException : ElementValidationException
    {
        public InvalidCharactersException(string name, DicomVR vr, string value) : base(name, vr, value, ValidationErrorCode.InvalidCharacters, DicomCoreResource.ErrorMessageInvalidCharacters)
        {
        }
    }
}
