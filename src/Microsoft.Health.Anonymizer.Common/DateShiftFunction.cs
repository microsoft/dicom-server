// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text;
using EnsureThat;
using Microsoft.Health.Anonymizer.Common.Exceptions;
using Microsoft.Health.Anonymizer.Common.Models;
using Microsoft.Health.Anonymizer.Common.Settings;
using Microsoft.Health.Anonymizer.Common.Utilities;

namespace Microsoft.Health.Anonymizer.Common
{
    public class DateShiftFunction
    {
        private const int DateShiftSeed = 131;
        private readonly DateShiftSetting _dateShiftSetting;

        public DateShiftFunction(DateShiftSetting dateShiftSetting)
        {
            EnsureArg.IsNotNull(dateShiftSetting, nameof(dateShiftSetting));

            _dateShiftSetting = dateShiftSetting;
        }

        public string Shift(string inputString, AnonymizerValueTypes valueType)
        {
            EnsureArg.IsNotNull(inputString, nameof(inputString));

            return valueType switch
            {
                AnonymizerValueTypes.Date => ShiftDateTime(inputString, DateTimeGlobalSettings.DateFormat),
                AnonymizerValueTypes.DateTime => ShiftDateTime(inputString, DateTimeGlobalSettings.DateTimeFormat),
                _ => throw new AnonymizerException(AnonymizerErrorCode.DateShiftFailed, "Unsupported value type. DateShift is only applicable to Date or DateTime values."),
            };
        }

        public DateTimeOffset Shift(DateTimeOffset dateTime)
        {
            EnsureArg.IsNotNull<DateTimeOffset>(dateTime, nameof(dateTime));

            try
            {
                DateTimeOffset newDateTime = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
                return newDateTime.AddDays(GetDateShiftValue());
            }
            catch (Exception ex)
            {
                throw new AnonymizerException(AnonymizerErrorCode.DateShiftFailed, "Failed to shift date.", ex);
            }
        }

        private string ShiftDateTime(string inputString, string dateTimeFormat)
        {
            DateTimeOffset date = DateTimeUtility.ParseDateTimeString(inputString, dateTimeFormat);
            return Shift(date).ToString(dateTimeFormat);
        }

        private int GetDateShiftValue()
        {
            int offset = 0;
            var bytes = Encoding.UTF8.GetBytes(_dateShiftSetting.DateShiftKeyPrefix + _dateShiftSetting.DateShiftKey);
            foreach (byte b in bytes)
            {
                offset = (int)(((offset * DateShiftSeed) + b) % ((2 * _dateShiftSetting.DateShiftRange) + 1));
            }

            offset -= (int)_dateShiftSetting.DateShiftRange;

            return offset;
        }

        public void SetDateShiftPrefix(string value)
        {
            _dateShiftSetting.DateShiftKeyPrefix = value ?? string.Empty;
        }
    }
}
