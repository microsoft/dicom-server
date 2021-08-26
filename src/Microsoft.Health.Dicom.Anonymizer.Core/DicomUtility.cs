// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dicom;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    /// <summary>
    /// Utility functions are used to parse DICOM data including DA, DT, AS. The format for these VR is given in DICOM standard.
    /// http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part05/sect_6.2.html
    /// </summary>
    public static class DicomUtility
    {
        private static readonly Dictionary<string, AgeType> AgeTypeMapping = new Dictionary<string, AgeType>
            {
                { "Y", AgeType.Year },
                { "M", AgeType.Month },
                { "W", AgeType.Week },
                { "D", AgeType.Day },
            };

        private const int AgeStringLength = 3;
        private const int MaxAgeValue = 999;

        public static DateTimeOffset[] ParseDicomDate(DicomDate item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return item.Get<string[]>().Select(ParseDicomDate).ToArray();
        }

        public static DateTimeOffset ParseDicomDate(string date)
        {
            EnsureArg.IsNotNull(date, nameof(date));

            try
            {
                return DateTimeOffset.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new DicomDataException("Invalid date value. The valid format is YYYYMMDD.", ex);
            }
        }

        public static DateTimeObject[] ParseDicomDateTime(DicomDateTime item)
        {
            EnsureArg.IsNotNull(item, nameof(item));

            return item.Get<string[]>().Select(ParseDicomDateTime).ToArray();
        }

        public static DateTimeObject ParseDicomDateTime(string dateTime)
        {
            EnsureArg.IsNotNull(dateTime, nameof(dateTime));

            // Reference link http://dicom.nema.org/medical/Dicom/2017e/output/chtml/part05/sect_6.2.html
            Regex dateTimeRegex = new Regex(@"^((?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<hour>\d{2})(?<minute>\d{2})(?<second>\d{2})(\.(?<microsecond>\d{1,6}))?(?<timeZone>(?<sign>-|\+)(?<timeZoneHour>\d{2})(?<timeZoneMinute>\d{2}))?)(\s*)$");
            var matches = dateTimeRegex.Matches(dateTime);
            if (matches.Count != 1)
            {
                throw new DicomDataException($"Invalid datetime value [{dateTime}]. The valid format is YYYYMMDDHHMMSS.FFFFFF&ZZXX.");
            }

            var groups = matches[0].Groups;

            int year = groups["year"].Success ? int.Parse(groups["year"].Value) : 1;
            int month = groups["month"].Success ? int.Parse(groups["month"].Value) : 1;
            int day = groups["day"].Success ? int.Parse(groups["day"].Value) : 1;
            int hour = groups["hour"].Success ? int.Parse(groups["hour"].Value) : 0;
            int minute = groups["minute"].Success ? int.Parse(groups["minute"].Value) : 0;
            int second = groups["second"].Success ? int.Parse(groups["second"].Value) : 0;
            int millisecond = groups["microsecond"].Success ? int.Parse(groups["microsecond"].Value) / 1000 : 0;

            if (groups["timeZone"].Success)
            {
                int sign = int.Parse(groups["sign"].Value + "1");
                int timeZoneHour = int.Parse(groups["timeZoneHour"].Value) * sign;
                int timeZoneMinute = int.Parse(groups["timeZoneMinute"].Value) * sign;
                return new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, new TimeSpan(timeZoneHour, timeZoneMinute, 0)),
                    HasTimeZone = true,
                };
            }
            else
            {
                return new DateTimeObject()
                {
                    DateValue = new DateTimeOffset(year, month, day, hour, minute, second, millisecond, default),
                    HasTimeZone = false,
                };
            }
        }

        public static string GenerateDicomDateString(DateTimeOffset date)
        {
            return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        public static string GenerateDicomDateTimeString(DateTimeObject date)
        {
            EnsureArg.IsNotNull(date, nameof(date));

            return date.HasTimeZone == true
                ? date.DateValue.ToString("yyyyMMddHHmmss.ffffffzzz", CultureInfo.InvariantCulture).Replace(":", string.Empty)
                : date.DateValue.ToString("yyyyMMddhhmmss.ffffff", CultureInfo.InvariantCulture);
        }

        public static AgeObject ParseAge(string age)
        {
            EnsureArg.IsNotNull(age, nameof(age));

            foreach (var item in AgeTypeMapping)
            {
                if (new Regex(@"^\d{3}" + item.Key + "$").IsMatch(age))
                {
                    return new AgeObject(uint.Parse(age.Substring(0, AgeStringLength)), item.Value);
                }
            }

            throw new DicomDataException($"Invalid age string [{age}]. The valid strings are nnnD, nnnW, nnnM, nnnY.");
        }

        public static string GenerateAgeString(AgeObject age)
        {
            EnsureArg.IsNotNull(age, nameof(age));

            foreach (var item in AgeTypeMapping)
            {
                if (age.AgeType == item.Value)
                {
                    // Age string only support 3 charaters
                    if (age.Value <= MaxAgeValue)
                    {
                        return age.Value.ToString().PadLeft(AgeStringLength, '0') + item.Key;
                    }
                    else if (age.AgeInYears() <= MaxAgeValue)
                    {
                        return age.AgeInYears().ToString().PadLeft(AgeStringLength, '0') + "Y";
                    }

                    throw new DicomDataException($"Invalid age value[{age.Value}] for DICOM. The valid strings are nnnD, nnnW, nnnM, nnnY.");
                }
            }

            return null;
        }

        public static void DisableAutoValidation(DicomDataset dataset)
        {
            // If users diable output validation, the output results should not strictly follow DICOM standard. Therefore, we need to diable auto-validation here.
#pragma warning disable CS0618 // Type or member is obsolete
            dataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
