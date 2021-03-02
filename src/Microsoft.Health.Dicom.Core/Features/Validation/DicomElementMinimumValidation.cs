// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    // TODO: for string contatins/not contains something, just verify max length?
    public static class DicomElementMinimumValidation
    {
        private static readonly Regex ValidIdentifierCharactersFormat = new Regex("^[0-9\\.]*$", RegexOptions.Compiled);
        private const string DateFormatForDA = "yyyyMMdd";

        internal static void ValidateCS(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringMaxLength(element, 16);
        }

        internal static void ValidateAT(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayLength(element, 4);
        }

        private static string GetName(DicomTag dicomTag) => dicomTag.IsPrivate ? dicomTag.GetPath() : dicomTag.DictionaryEntry.Keyword;

        private static void ValidateStringMaxLength(DicomElement dicomElement, int maxLength)
        {
            string value = dicomElement.Get<string>();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.Length > maxLength)
            {
                throw new DicomElementValidationException(GetName(dicomElement.Tag), value, dicomElement.ValueRepresentation, $"Value exceeds maximum length of {maxLength} characters.");
            }
        }

        private static void ValidateStringLength(DicomElement dicomElement, int length)
        {
            string value = dicomElement.Get<string>();

            if (value.Length != length)
            {
                throw new DicomElementValidationException(GetName(dicomElement.Tag), value, dicomElement.ValueRepresentation, $"Element must be length of {length}");
            }
        }

        private static void ValidateByteArrayMaxLength(DicomElement dicomElement, int maxLength)
        {
            if (dicomElement.Buffer.Size > maxLength)
            {
                // TODO: value should not be required.
                throw new DicomElementValidationException(GetName(dicomElement.Tag), "<Byte Array>", dicomElement.ValueRepresentation, $"Value exceeds maximum length of {maxLength}.");
            }
        }

        private static void ValidateByteArrayLength(DicomElement dicomElement, int length)
        {
            if (dicomElement.Buffer.Size != length)
            {
                // TODO: value should not be required.
                throw new DicomElementValidationException(GetName(dicomElement.Tag), "<Byte Array>", dicomElement.ValueRepresentation, $"Value exceeds maximum length of {length}.");
            }
        }

        private static void ValidateStringNotContains(DicomElement dicomElement, char[] invalidChars)
        {
            string value = dicomElement.Get<string>();
            if (value.ToCharArray().Any(chr => invalidChars.Contains(chr)))
            {
                throw new DicomElementValidationException(GetName(dicomElement.Tag), value, dicomElement.ValueRepresentation, DicomCoreResource.ValueContainsInvalidCharacter);
            }
        }

        private static void ValidateStringOnlyContains(DicomElement dicomElement, char[] validChars)
        {
            string value = dicomElement.Get<string>();
            if (!value.ToCharArray().All(chr => validChars.Contains(chr)))
            {
                throw new DicomElementValidationException(GetName(dicomElement.Tag), value, dicomElement.ValueRepresentation, DicomCoreResource.ValueContainsInvalidCharacter);
            }
        }

        private static void ValidateStringAsDate(DicomElement dicomElement, string dateFormat)
        {
            string value = dicomElement.Get<string>();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!DateTime.TryParseExact(value, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _))
            {
                throw new DicomElementValidationException(GetName(dicomElement.Tag), value, DicomVR.DT, DicomCoreResource.ValueIsInvalidDate);
            }
        }

        internal static void ValidateAE(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));

            // Default Character Repertoire excluding character code 5CH (the BACKSLASH "\" in ISO-IR 6), and control characters LF, FF, CR and ESC.
            // Notes: LF - NewLine (12), FF - PageBreak(14), CR - CarriageReturn (15), ESC - 13
            ValidateStringNotContains(element, new char[] { SpecialChars.Backslash, SpecialChars.LineFeed, SpecialChars.PageBreak, SpecialChars.CarriageReturn, SpecialChars.Escape });
            ValidateStringMaxLength(element, 16);
        }

        internal static void ValidateDS(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringOnlyContains(element, "0123456789+-Ee.".ToCharArray());
            ValidateStringMaxLength(element, 16);
        }

        internal static void ValidateDT(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            try
            {
                element.Validate();
            }
            catch (DicomValidationException)
            {
                throw new DicomElementValidationException(GetName(element.Tag), element.Get<string>(), DicomVR.DT, DicomCoreResource.ValueIsInvalidDate);
            }
        }

        internal static void ValidateFD(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayMaxLength(element, 8);
        }

        internal static void ValidateIS(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringMaxLength(element, 12);
            ValidateStringOnlyContains(element, "0123456789+-".ToCharArray());
        }

        internal static void ValidateAS(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringLength(element, 4);
            ValidateStringOnlyContains(element, "0123456789DWMY".ToCharArray());
        }

        internal static void ValidateSL(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayLength(element, 4);
        }

        internal static void ValidateTM(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            try
            {
                element.Validate();
            }
            catch (DicomValidationException)
            {
                throw new DicomElementValidationException(GetName(element.Tag), element.Get<string>(), element.ValueRepresentation, DicomCoreResource.ValueIsInvalidDate);
            }
        }

        internal static void ValidateSS(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayLength(element, 2);
        }

        internal static void ValidateUS(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayLength(element, 2);
        }

        internal static void ValidateUL(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayLength(element, 4);
        }

        internal static void ValidateFL(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateByteArrayLength(element, 4);
        }

        public static void ValidateDA(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringAsDate(element, DateFormatForDA);
        }

        public static void ValidateLO(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringMaxLength(element, 64);

            string value = element.Get<string>();

            if (value.Contains("\\", System.StringComparison.OrdinalIgnoreCase) || value.ToCharArray().Any(IsControlExceptESC))
            {
                throw new DicomElementValidationException(GetName(element.Tag), value, DicomVR.LO, DicomCoreResource.ValueContainsInvalidCharacter);
            }
        }

        // probably can dial down the validation here
        public static void ValidatePN(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            string name = GetName(element.Tag);
            string value = element.Get<string>();
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

        public static void ValidateSH(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            ValidateStringMaxLength(element, 16);
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

        public static void ValidateUI(DicomElement element)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            string name = GetName(element.Tag);
            string value = element.Get<string>();
            ValidateUI(value, name);
        }

        private static bool IsControlExceptESC(char c)
            => char.IsControl(c) && (c != '\u001b');
    }
}
