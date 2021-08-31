// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Exceptions.Validation
{
    public class ExceedMaxLengthException : ElementValidationException
    {
        public ExceedMaxLengthException(string name, DicomVR vr, string value, int maxlength) :
            base(
                    name,
                    vr,
                    value,
                    ValidationErrorCode.ExceedMaxLength,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageExceedMaxLength, maxlength)
                )
        {
            Maxlength = maxlength;
        }

        public int Maxlength { get; }
    }
}
