// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Models;
using Microsoft.Health.Anonymizer.Common.Settings;
using Microsoft.Health.Anonymizer.Common.Utilities;

namespace Microsoft.Health.Anonymizer.Common
{
    public class RedactFunction
    {
        private const string PostalCodeReplacementDigit = "0";
        private const int InitialDigitsCount = 3;
        private readonly RedactSetting _redactSetting;

        public RedactFunction(RedactSetting redactSetting)
        {
            EnsureArg.IsNotNull(redactSetting, nameof(redactSetting));

            _redactSetting = redactSetting ?? new RedactSetting();
        }

        public string Redact(string inputString, AnonymizerValueTypes valueType)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return null;
            }

            return valueType switch
            {
                AnonymizerValueTypes.Date => RedactDateTime(inputString, DateTimeGlobalSettings.DateFormat),
                AnonymizerValueTypes.DateTime => RedactDateTime(inputString, DateTimeGlobalSettings.DateTimeFormat),
                AnonymizerValueTypes.Age => RedactAge(inputString),
                AnonymizerValueTypes.PostalCode => RedactPostalCode(inputString),
                _ => null,
            };
        }

        public uint? Redact(uint value, AnonymizerValueTypes valueType)
        {
            return valueType switch
            {
                AnonymizerValueTypes.Age => RedactAge(value),
                _ => null,
            };
        }

        public decimal? Redact(decimal value, AnonymizerValueTypes valueType)
        {
            return valueType switch
            {
                AnonymizerValueTypes.Age => RedactAge(value),
                _ => null,
            };
        }

        public DateTimeOffset? Redact(DateTimeOffset dateTime)
        {
            EnsureArg.IsNotNull<DateTimeOffset>(dateTime, nameof(dateTime));

            if (_redactSetting.EnablePartialDatesForRedact && !DateTimeUtility.IsAgeOverThreshold(dateTime))
            {
                return new DateTimeOffset(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Offset);
            }

            return null;
        }

        public DateTimeObject Redact(DateTimeObject dateObject)
        {
            EnsureArg.IsNotNull(dateObject, nameof(dateObject));

            if (_redactSetting.EnablePartialDatesForRedact && !DateTimeUtility.IsAgeOverThreshold(dateObject.DateValue))
            {
                dateObject.DateValue = new DateTimeOffset(dateObject.DateValue.Year, 1, 1, 0, 0, 0, dateObject.HasTimeZone == true ? dateObject.DateValue.Offset : default);
                return dateObject;
            }

            return null;
        }

        public AgeObject Redact(AgeObject age)
        {
            EnsureArg.IsNotNull(age, nameof(age));

            return RedactAge(age.AgeInYears()) == null ? null : age;
        }

        private string RedactDateTime(string dateTimeString, string dateTimeFormat)
        {
            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(dateTimeString, dateTimeFormat);
            return Redact(date)?.ToString(dateTimeFormat);
        }

        private string RedactAge(string age)
        {
            try
            {
                return RedactAge(decimal.Parse(age)).ToString();
            }
            catch
            {
                throw new AnonymizerException(AnonymizerErrorCode.RedactFailed, "The input value is not a numeric age value.");
            }
        }

        private uint? RedactAge(uint age)
        {
            return (uint?)RedactAge((decimal)age);
        }

        private decimal? RedactAge(decimal age)
        {
            if (_redactSetting.EnablePartialAgesForRedact && age <= DateTimeGlobalSettings.AgeThreshold)
            {
                return age;
            }

            return null;
        }

        private string RedactPostalCode(string postalCode)
        {
            if (_redactSetting.EnablePartialZipCodesForRedact)
            {
                if (_redactSetting.RestrictedZipCodeTabulationAreas != null && _redactSetting.RestrictedZipCodeTabulationAreas.Any(x => postalCode.StartsWith(x)))
                {
                    postalCode = Regex.Replace(postalCode, @"\d", PostalCodeReplacementDigit);
                }
                else if (postalCode.Length >= InitialDigitsCount)
                {
                    var suffix = postalCode[InitialDigitsCount..];
                    postalCode = $"{postalCode.Substring(0, InitialDigitsCount)}{Regex.Replace(suffix, @"\d", PostalCodeReplacementDigit)}";
                }

                return postalCode;
            }

            return null;
        }
    }
}
