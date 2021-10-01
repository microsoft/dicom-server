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
    internal class TimeValidation : ElementValidation
    {
        private static readonly string[] TimeFormatsTM =
        {
            "HHmmss.FFFFFF",
            "HHmmss",
            "HHmm",
            "HH"
        };

        public override void Validate(DicomElement dicomElement)
        {
            base.Validate(dicomElement);

            string value = dicomElement.Get<string>();
            string name = dicomElement.Tag.GetFriendlyName();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!DateTime.TryParseExact(value, TimeFormatsTM, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out _))
            {
                throw ElementValidationExceptionFactory.CreateDateIsInvalidException(name, value);
            }
        }
    }
}
