// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Core.Features.Query;

/// <summary>
/// Value parsers
/// </summary>
public abstract class BaseQueryParser<TQueryExpression, TQueryParameters> : IQueryParser<TQueryExpression, TQueryParameters>
     where TQueryExpression : BaseQueryExpression
     where TQueryParameters : BaseQueryParameters
{
    protected const string IncludeFieldValueAll = "all";

    private readonly static Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>> ValueParsers = new Dictionary<DicomVR, Func<QueryTag, string, QueryFilterCondition>>
    {
        { DicomVR.DA, ParseDateOrTimeTagValue },
        { DicomVR.DT, ParseDateOrTimeTagValue },
        { DicomVR.TM, ParseTimeTagValue },

        { DicomVR.UI, ParseStringTagValue },
        { DicomVR.LO, ParseStringTagValue },
        { DicomVR.SH, ParseStringTagValue },
        { DicomVR.PN, ParseStringTagValue },
        { DicomVR.CS, ParseStringTagValue },

        { DicomVR.AE, ParseStringTagValue },
        { DicomVR.AS, ParseStringTagValue },
        { DicomVR.DS, ParseStringTagValue },
        { DicomVR.IS, ParseStringTagValue },

        { DicomVR.SL, ParseLongTagValue },
        { DicomVR.SS, ParseLongTagValue },
        { DicomVR.UL, ParseLongTagValue },
        { DicomVR.US, ParseLongTagValue },

        { DicomVR.FL, ParseDoubleTagValue },
        { DicomVR.FD, ParseDoubleTagValue },
    };

    public const string DateTagValueFormat = "yyyyMMdd";

    public static readonly string[] DateTimeTagValueFormats =
    {
        "yyyyMMddHHmmss.FFFFFF",
        "yyyyMMddHHmmss",
        "yyyyMMddHHmm",
        "yyyyMMddHH",
        "yyyyMMdd",
        "yyyyMM",
        "yyyy"
    };

    public static readonly string[] DateTimeTagValueWithOffsetFormats =
    {
        "yyyyMMddHHmmss.FFFFFFzzz",
        "yyyyMMddHHmmsszzz",
        "yyyyMMddHHmmzzz",
        "yyyyMMddHHzzz",
        "yyyyMMddzzz",
        "yyyyMMzzz",
        "yyyyzzz"
    };

    public abstract TQueryExpression Parse(TQueryParameters parameters, IReadOnlyCollection<QueryTag> queryTags);

    protected static bool TryGetValueParser(QueryTag tag, bool fuzzyMatching, out Func<QueryTag, string, QueryFilterCondition> valueParseFunc)
    {
        EnsureArg.IsNotNull(tag, nameof(tag));
        valueParseFunc = null;
        if (ValueParsers.TryGetValue(tag.VR, out Func<QueryTag, string, QueryFilterCondition> valueParser))
        {
            valueParseFunc = valueParser;
            if (fuzzyMatching && QueryLimit.IsValidFuzzyMatchingQueryTag(tag))
            {
                valueParseFunc = ParseFuzzyMatchingTagValue;
            }
        }
        return valueParseFunc != null;
    }

    private static PersonNameFuzzyMatchCondition ParseFuzzyMatchingTagValue(QueryTag queryTag, string value)
    {
        // quotes is not supported in fuzzy matching because of limitation in underlaying implementation
        char unspportedChar = '"';
        if (value.Contains(unspportedChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new QueryParseException(string.Format(
                CultureInfo.CurrentCulture,
                DicomCoreResource.InvalidTagValueWithFuzzyMatch,
                queryTag.GetName(),
                value,
                unspportedChar));
        }

        return new PersonNameFuzzyMatchCondition(queryTag, value);
    }

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

                // Make sure both parts of the range values are not empty.
                // If so, throw an exception.
                ValidateEmptyValuesForRangeQuery(minDateTime, maxDateTime);

                DateTime parsedMinDateTime = string.IsNullOrEmpty(minDateTime) ? DateTime.MinValue : parseValue(minDateTime, queryTag.GetName());
                DateTime parsedMaxDateTime = string.IsNullOrEmpty(maxDateTime) ? DateTime.MaxValue : parseValue(maxDateTime, queryTag.GetName());

                if (parsedMinDateTime > parsedMaxDateTime)
                {
                    throw new QueryParseException(string.Format(
                        CultureInfo.CurrentCulture,
                        exceptionBaseMessage,
                        value,
                        minDateTime,
                        maxDateTime));
                }

                return new DateRangeValueMatchCondition(queryTag, parsedMinDateTime, parsedMaxDateTime);
            }
        }

        DateTime parsedDateTime = parseValue(value, queryTag.GetName());
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

                // Make sure both parts of the range values are not empty.
                // If so, throw an exception.
                ValidateEmptyValuesForRangeQuery(minTime, maxTime);

                long parsedMinTime = string.IsNullOrEmpty(minTime) ? 0 : ParseTime(minTime, queryTag);
                long parsedMaxTime = string.IsNullOrEmpty(maxTime) ? TimeSpan.TicksPerDay : ParseTime(maxTime, queryTag);

                if (parsedMinTime > parsedMaxTime)
                {
                    throw new QueryParseException(string.Format(
                        CultureInfo.CurrentCulture,
                        DicomCoreResource.InvalidTimeRangeValue,
                        value,
                        minTime,
                        maxTime));
                }

                return new LongRangeValueMatchCondition(queryTag, parsedMinTime, parsedMaxTime);
            }
        }

        long parsedTime = ParseTime(value, queryTag);
        return new LongSingleValueMatchCondition(queryTag, parsedTime);
    }

    private static QueryFilterCondition ParseStringTagValue(QueryTag queryTag, string value)
    {
        if (QueryLimit.IsStudyToSeriesTag(queryTag.Tag))
        {
            return new StudyToSeriesStringSingleValueMatchCondition(queryTag, value);
        }
        return new StringSingleValueMatchCondition(queryTag, value);
    }

    private static QueryFilterCondition ParseDoubleTagValue(QueryTag queryTag, string value)
    {
        if (!double.TryParse(value, out double val))
        {
            throw new QueryParseException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidDoubleValue, value, queryTag.GetName()));
        }

        return new DoubleSingleValueMatchCondition(queryTag, val);
    }

    private static QueryFilterCondition ParseLongTagValue(QueryTag queryTag, string value)
    {
        if (!long.TryParse(value, out long val))
        {
            throw new QueryParseException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidLongValue, value, queryTag.GetName()));
        }

        return new LongSingleValueMatchCondition(queryTag, val);
    }

    private static DateTime ParseDate(string date, string tagName)
    {
        if (!DateTime.TryParseExact(date, DateTagValueFormat, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
        {
            throw new QueryParseException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidDateValue, date, tagName));
        }

        return parsedDate;
    }

    private static DateTime ParseDateTime(string dateTime, string tagName)
    {
        // If the attribute has + in the value (for offset), it is converted to space by default.
        // Encoding it to get the + back such that it can be parsed as an offset properly.
        string encodedDateTime = WebUtility.UrlEncode(dateTime);

        if (!DateTime.TryParseExact(dateTime, DateTimeTagValueFormats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
        {
            if (DateTime.TryParseExact(encodedDateTime, DateTimeTagValueWithOffsetFormats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTimeOffsetNotSupported))
            {
                throw new QueryParseException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.DateTimeWithOffsetNotSupported, encodedDateTime, tagName));
            }
            else
            {
                throw new QueryParseException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidDateTimeValue, encodedDateTime, tagName));
            }
        }

        return parsedDateTime;
    }

    private static long ParseTime(string time, QueryTag queryTag)
    {
        DateTime timeValue;

        try
        {
            DicomTime dicomTime = new DicomTime(queryTag.Tag, new string[] { time });
            timeValue = dicomTime.Get<DateTime>();
        }
        catch (Exception)
        {
            throw new QueryParseException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidTimeValue, time, queryTag.GetName()));
        }

        return timeValue.Ticks;
    }

    /// <summary>
    /// Validate if both values for range query are not empty.
    /// If so, throws an exception.
    /// </summary>
    /// <param name="value1">Value 1 of range query.</param>
    /// <param name="value2">Value 2 of range query.</param>
    private static void ValidateEmptyValuesForRangeQuery(string value1, string value2)
    {
        if (string.IsNullOrEmpty(value1) && string.IsNullOrEmpty(value2))
        {
            throw new QueryParseException(DicomCoreResource.InvalidRangeValues);
        }
    }
}
