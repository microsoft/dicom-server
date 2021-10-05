// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Value parsers
    /// </summary>
    public partial class QueryParser
    {
        private static QueryFilterCondition ParseDateOrTimeTagValue(QueryTag queryTag, string value)
        {
            QueryFilterCondition queryFilterCondition = null;

            if (queryTag.VR == DicomVR.DT)
            {
                queryFilterCondition = ParseDateOrTimeTagValue(queryTag, value, DicomCoreResource.InvalidDateTimeRangeValue, ParseDateTime);
            }
            else
            {
                queryFilterCondition = ParseDateOrTimeTagValue(queryTag, value, DicomCoreResource.InvalidDateRangeValue, ParseDate);
            }

            return queryFilterCondition;
        }

        private static QueryFilterCondition ParseDateOrTimeTagValue(QueryTag queryTag, string value, string exceptionBaseMessage, Func<string, string, DateTime> parseValue)
        {
            if (QueryLimit.IsValidRangeQueryTag(queryTag))
            {
                var splitString = value.Split('-');
                if (splitString.Length == 2)
                {
                    string minDateTime = splitString[0].Trim();
                    string maxDateTime = splitString[1].Trim();
                    DateTime parsedMinDateTime = parseValue(minDateTime, queryTag.GetName());
                    DateTime parsedMaxDateTime = parseValue(maxDateTime, queryTag.GetName());

                    if (parsedMinDateTime > parsedMaxDateTime)
                    {
                        throw new QueryParseException(string.Format(
                            exceptionBaseMessage,
                            value,
                            minDateTime,
                            maxDateTime));
                    }

                    return new DateRangeValueMatchCondition(queryTag, parsedMinDateTime, parsedMaxDateTime);
                }
            }

            DateTime parsedDateTime = ParseDateTime(value, queryTag.GetName());
            return new DateSingleValueMatchCondition(queryTag, parsedDateTime);
        }

        private static QueryFilterCondition ParseTimeTagValue(QueryTag queryTag, string value)
        {
            if (QueryLimit.IsValidRangeQueryTag(queryTag))
            {
                var splitString = value.Split('-');
                if (splitString.Length == 2)
                {
                    string minTime = splitString[0].Trim();
                    string maxTime = splitString[1].Trim();
                    long parsedMinTime = ParseTime(minTime, queryTag.GetName());
                    long parsedMaxTime = ParseTime(maxTime, queryTag.GetName());

                    if (parsedMinTime > parsedMaxTime)
                    {
                        throw new QueryParseException(string.Format(
                            DicomCoreResource.InvalidTimeRangeValue,
                            value,
                            minTime,
                            maxTime));
                    }

                    return new LongRangeValueMatchCondition(queryTag, parsedMinTime, parsedMaxTime);
                }
            }

            long parsedTime = ParseTime(value, queryTag.GetName());
            return new LongSingleValueMatchCondition(queryTag, parsedTime);
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
            // If the attribute has + in the value (for offset), it is converted to space by default.
            // Encoding it to get the + back such that it can be parsed as an offset properly.
            string encodedDateTime = WebUtility.UrlEncode(dateTime);

            if (!DateTimeOffset.TryParseExact(dateTime, DateTimeTagValueFormats, null, System.Globalization.DateTimeStyles.None, out DateTimeOffset parsedDateTimeOffset))
            {
                if (DateTimeOffset.TryParseExact(encodedDateTime, DateTimeTagValueWithOffsetFormats, null, System.Globalization.DateTimeStyles.None, out DateTimeOffset parsedDateTimeOffsetNotSupported))
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.DateTimeWithOffsetNotSupported, encodedDateTime, tagName));
                }
                else
                {
                    throw new QueryParseException(string.Format(DicomCoreResource.InvalidDateTimeValue, encodedDateTime, tagName));
                }
            }

            return parsedDateTimeOffset.DateTime;
        }

        private static long ParseTime(string time, string tagName)
        {
            if (!DateTime.TryParseExact(time, TimeTagValueFormats, null, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out DateTime parsedTime))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidTimeValue, time, tagName));
            }

            return parsedTime.Ticks;
        }
    }
}
