// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Shared;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public static class DicomElementMinimumValidation
    {
        private static readonly Regex ValidIdentifierCharactersFormat = new Regex("^[0-9\\.]*$", RegexOptions.Compiled);
        private const string DateFormatDA = "yyyyMMdd";
        private const string BinaryDataPlaceHolder = "<BinaryData>";

        internal static void ValidateAE(string value, string name)
        {
            ValidateLength(value.Length, 0, 16, DicomVR.AE, name, value);
        }

        internal static void ValidateAS(string value, string name)
        {
            ValidateLength(value.Length, 4, 4, DicomVR.AE, name, value);
        }

        internal static void ValidateCS(string value, string name)
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

        internal static void ValidateDA(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!DateTime.TryParseExact(value, DateFormatDA, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _))
            {
                throw new DicomElementValidationException(name, value, DicomVR.DA, DicomCoreResource.ValueIsInvalidDate);
            }
        }

        internal static void ValidateDS(string value, string name)
        {
            ValidateLength(value.Length, 0, 16, DicomVR.DS, name, value);
        }

        internal static void ValidateFL(IByteBuffer value, string name)
        {
            ValidateLength(value.Size, 4, 4, DicomVR.FL, name, BinaryDataPlaceHolder);
        }

        internal static void ValidateFD(IByteBuffer value, string name)
        {
            ValidateLength(value.Size, 8, 8, DicomVR.FD, name, BinaryDataPlaceHolder);
        }

        internal static void ValidateIS(string value, string name)
        {
            ValidateLength(value.Length, 0, 12, DicomVR.IS, name, value);
        }

        internal static void ValidateLO(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.Length > 64)
            {
                throw new DicomElementValidationException(name, value, DicomVR.LO, DicomCoreResource.ValueLengthExceeds64Characters);
            }

            if (value.Contains("\\", StringComparison.OrdinalIgnoreCase) || value.ToCharArray().Any(IsControlExceptESC))
            {
                throw new DicomElementValidationException(name, value, DicomVR.LO, DicomCoreResource.ValueContainsInvalidCharacter);
            }
        }

        // probably can dial down the validation here
        internal static void ValidatePN(string value, string name)
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

        internal static void ValidateSH(string value, string name)
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

        internal static void ValidateSL(IByteBuffer value, string name)
        {
            ValidateLength(value.Size, 4, 4, DicomVR.SL, name, BinaryDataPlaceHolder);
        }

        internal static void ValidateSS(IByteBuffer value, string name)
        {
            ValidateLength(value.Size, 2, 2, DicomVR.SS, name, BinaryDataPlaceHolder);
        }

        internal static void ValidateUI(string value, string name)
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

        internal static void ValidateUL(IByteBuffer value, string name)
        {
            ValidateLength(value.Size, 4, 4, DicomVR.UL, name, BinaryDataPlaceHolder);
        }

        internal static void ValidateUS(IByteBuffer value, string name)
        {
            ValidateLength(value.Size, 2, 2, DicomVR.US, name, BinaryDataPlaceHolder);
        }

        private static void ValidateLength(long actualLength, long minLength, long maxLength, DicomVR dicomVR, string name, string valueContent)
        {
            if (actualLength < minLength || actualLength > maxLength)
            {
                if (minLength == maxLength)
                {
                    throw new DicomElementValidationException(
                        name,
                        valueContent,
                        dicomVR,
                        string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthIsNotRequiredLength, minLength));
                }
                else
                {
                    if (actualLength < minLength)
                    {
                        throw new DicomElementValidationException(
                            name,
                            valueContent,
                            dicomVR,
                            string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthBelowMinLength, minLength));
                    }
                    else
                    {
                        throw new DicomElementValidationException(
                            name,
                            valueContent,
                            dicomVR,
                            string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ValueLengthAboveMaxLength, maxLength));
                    }
                }
            }
        }

        private static bool IsControlExceptESC(char c)
            => char.IsControl(c) && (c != '\u001b');
    }
}
