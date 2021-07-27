// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    internal class MaxLengthValidation : IValidationRule
    {
        public MaxLengthValidation(int maxLength, ValidationErrorCode errorCode)
        {
            MaxLength = maxLength;
            ErrorCode = errorCode;
        }

        public int MaxLength { get; }
        public ValidationErrorCode ErrorCode { get; }

        public void Validate(DicomElement dicomElement)
        {
            string value = dicomElement.Get<string>();
            if (value?.Length > MaxLength)
            {
                throw new DicomStringElementValidationException(
                    ErrorCode,
                    dicomElement.Tag.GetFriendlyName(),
                    value,
                    dicomElement.ValueRepresentation,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthAboveMaxLength, MaxLength));
            }
        }
    }
}
