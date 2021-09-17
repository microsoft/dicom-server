// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    internal static class QueryTagValueParser
    {
        public const string DateTagValueFormat = "yyyyMMdd";

        private delegate QueryFilterCondition ValueParserFunc(QueryTag queryTag, string value);
        private static readonly Dictionary<DicomVR, ValueParserFunc> ValueParsers = new Dictionary<DicomVR, ValueParserFunc>();

        static QueryTagValueParser()
        {
            // register value parsers
            ValueParsers.Add(DicomVR.DA, ParseDateTagValue);
            ValueParsers.Add(DicomVR.UI, ParseStringTagValue);
            ValueParsers.Add(DicomVR.LO, ParseStringTagValue);
            ValueParsers.Add(DicomVR.SH, ParseStringTagValue);
            ValueParsers.Add(DicomVR.PN, ParseStringTagValue);
            ValueParsers.Add(DicomVR.CS, ParseStringTagValue);

            ValueParsers.Add(DicomVR.AE, ParseStringTagValue);
            ValueParsers.Add(DicomVR.AS, ParseStringTagValue);
            ValueParsers.Add(DicomVR.DS, ParseStringTagValue);
            ValueParsers.Add(DicomVR.IS, ParseStringTagValue);

            ValueParsers.Add(DicomVR.SL, ParseLongTagValue);
            ValueParsers.Add(DicomVR.SS, ParseLongTagValue);
            ValueParsers.Add(DicomVR.UL, ParseLongTagValue);
            ValueParsers.Add(DicomVR.US, ParseLongTagValue);

            ValueParsers.Add(DicomVR.FL, ParseDoubleTagValue);
            ValueParsers.Add(DicomVR.FD, ParseDoubleTagValue);
        }

        public static bool TryParseTagValue(QueryTag queryTag, string value, out QueryFilterCondition condition)
        {
            condition = null;
            if (ValueParsers.TryGetValue(queryTag.VR, out ValueParserFunc valueParser))
            {
                condition = valueParser(queryTag, value);
                return true;
            }
            return false;
        }

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

                    return new DateRangeValueMatchCondition(queryTag, parsedMinDate, parsedMaxDate);
                }
            }

            DateTime parsedDate = ParseDate(value, queryTag.GetName());
            return new DateSingleValueMatchCondition(queryTag, parsedDate);
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
    }
}
