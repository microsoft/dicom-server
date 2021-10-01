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
    internal class DateTimeValidation : ElementValidation
    {
        private static readonly string[] DateTimeFormatsDT =
        {
            "yyyyMMddHHmmss.FFFFFFzzz",
            "yyyyMMddHHmmsszzz",
            "yyyyMMddHHmmzzz",
            "yyyyMMddHHzzz",
            "yyyyMMddzzz",
            "yyyyMMzzz",
            "yyyyzzz",
            "yyyyMMddHHmmss.FFFFFF",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddHH",
            "yyyyMMdd",
            "yyyyMM",
            "yyyy"
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

            if (!DateTimeOffset.TryParseExact(value, DateTimeFormatsDT, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                throw ElementValidationExceptionFactory.CreateDateIsInvalidException(name, value);
            }
        }
    }
}
