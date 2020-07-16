// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public static class DicomElementMinimumValidation
    {
        private static readonly Regex ValidIdentifierCharactersFormat = new Regex("^[0-9\\.]*$", RegexOptions.Compiled);
        private const string DateFormat = "yyyyMMdd";

        public static void ValidateCS(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.Length > 16)
            {
                throw new DicomElementValidationException(name, value, DicomVR.CS, DicomCoreResource.ValueLengthExceeds16Characters);
            }
        }

        public static void ValidateDA(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _))
            {
                throw new DicomElementValidationException(name, value, DicomVR.DA, DicomCoreResource.ValueIsInvalidDate);
            }
        }

        public static void ValidateLO(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.Length > 64)
            {
                throw new DicomElementValidationException(name, value, DicomVR.LO, DicomCoreResource.ValueLengthExceeds64Characters);
            }

            if (value.Contains("\\", System.StringComparison.OrdinalIgnoreCase) || value.ToCharArray().Any(IsControlExceptESC))
            {
                throw new DicomElementValidationException(name, value, DicomVR.LO, DicomCoreResource.ValueContainsInvalidCharacter);
            }
        }

        // probably can dial down the validation here
        public static void ValidatePN(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                // empty values allowed
                return;
            }

            var groups = value.Split('=');
            if (groups.Length > 3)
            {
                throw new DicomElementValidationException(name, value, DicomVR.PN, "value contains too many groups");
            }

            foreach (var group in groups)
            {
                if (group.Length > 64)
                {
                    throw new DicomElementValidationException(name, value, DicomVR.PN, "value exceeds maximum length of 64 characters");
                }

                if (group.ToCharArray().Any(IsControlExceptESC))
                {
                    throw new DicomElementValidationException(name, value, DicomVR.PN, "value contains invalid control character");
                }
            }

            var groupcomponents = groups.Select(group => group.Split('^').Length);
            if (groupcomponents.Any(l => l > 5))
            {
                throw new DicomElementValidationException(name, value, DicomVR.PN, "value contains too many components");
            }
        }

        public static void ValidateSH(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.Length > 16)
            {
                throw new DicomElementValidationException(name, value, DicomVR.SH, DicomCoreResource.ValueLengthExceeds16Characters);
            }
        }

        public static void ValidateUI(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            // trailling spaces are allowed
            value = value.TrimEnd(' ');

            if (value.Length > 64)
            {
                // UI value is validated in other cases like params for WADO, DELETE. So keeping the exception specific.
                throw new InvalidIdentifierException(value, name);
            }

            if (!ValidIdentifierCharactersFormat.IsMatch(value))
            {
                throw new InvalidIdentifierException(value, name);
            }
        }

        private static bool IsControlExceptESC(char c)
            => char.IsControl(c) && (c != '\u001b');
    }
}
