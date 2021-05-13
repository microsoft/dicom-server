// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Value parsers
    /// </summary>
    public partial class QueryParser
    {
        private static QueryFilterCondition ParseDateTagValue(QueryTag queryTag, string value)
        {
            if (QueryLimit.IsValidRangeQueryTag(queryTag))
            {
                var splitString = value.Split('-');
                if (splitString.Length == 2)
                {
                    string minDate = splitString[0].Trim();
                    string maxDate = splitString[1].Trim();
                    DateTime parsedMinDate = ParseDate(minDate, queryTag.GetName());
                    DateTime parsedMaxDate = ParseDate(maxDate, queryTag.GetName());

                    if (parsedMinDate > parsedMaxDate)
                    {
                        throw new QueryParseException(string.Format(
                            DicomCoreResource.InvalidDateRangeValue,
                            value,
                            minDate,
                            maxDate));
                    }

                    return new DateTimeRangeValueMatchCondition(queryTag, parsedMinDate, parsedMaxDate);
                }
            }

            DateTime parsedDate = ParseDate(value, queryTag.GetName());
            return new DateTimeSingleValueMatchCondition(queryTag, parsedDate);
        }

        private static QueryFilterCondition ParseDateTimeTagValue(QueryTag queryTag, string value)
        {
            if (QueryLimit.IsValidRangeQueryTag(queryTag))
            {
                var splitString = value.Split('-');
                if (splitString.Length == 2)
                {
                    string minDate = splitString[0].Trim();
                    string maxDate = splitString[1].Trim();
                    DateTime parsedMinDate = ParseDateTime(minDate, queryTag.GetName());
                    DateTime parsedMaxDate = ParseDateTime(maxDate, queryTag.GetName());

                    if (parsedMinDate > parsedMaxDate)
                    {
                        throw new QueryParseException(string.Format(
                            DicomCoreResource.InvalidDateTimeRangeValue,
                            value,
                            minDate,
                            maxDate));
                    }

                    return new DateTimeRangeValueMatchCondition(queryTag, parsedMinDate, parsedMaxDate);
                }
            }

            DateTime parsedDate = ParseDateTime(value, queryTag.GetName());
            return new DateTimeSingleValueMatchCondition(queryTag, parsedDate);
        }

        private static QueryFilterCondition ParseStringTagValue(QueryTag queryTag, string value)
        {
            return new StringSingleValueMatchCondition(queryTag, value);
        }

        private static QueryFilterCondition ParseDoubleTagValue(QueryTag queryTag, string value)
        {
            if (!double.TryParse(value, out double val))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidDoubleValue, value, queryTag.GetName()));
            }

            return new DoubleSingleValueMatchCondition(queryTag, val);
        }

        private static QueryFilterCondition ParseLongTagValue(QueryTag queryTag, string value)
        {
            if (!long.TryParse(value, out long val))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidLongValue, value, queryTag.GetName()));
            }

            return new LongSingleValueMatchCondition(queryTag, val);
        }

        private static DateTime ParseDate(string date, string tagName)
        {
            if (!DateTime.TryParseExact(date, DateTagValueFormat, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidDateValue, date, tagName));
            }

            return parsedDate;
        }

        private static DateTime ParseDateTime(string dateTime, string tagName)
        {
            if (!DateTime.TryParseExact(dateTime, DateTimeTagValueFormat, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidDateTimeValue, dateTime, tagName));
            }

            return parsedDateTime;
        }
    }
}
