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
    internal static class QueryFilterConditionParser
    {
        public const string DateTagValueFormat = "yyyyMMdd";

        private delegate QueryFilterCondition ConditionParserFunc(QueryTag queryTag, string value);
        private static readonly Dictionary<DicomVR, ConditionParserFunc> ConditionParsers = new Dictionary<DicomVR, ConditionParserFunc>();

        static QueryFilterConditionParser()
        {
            // register value parsers
            ConditionParsers.Add(DicomVR.DA, ParseDateValueMatchCondition);
            ConditionParsers.Add(DicomVR.UI, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.LO, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.SH, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.PN, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.CS, ParseStringValueMatchCondition);

            ConditionParsers.Add(DicomVR.AE, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.AS, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.DS, ParseStringValueMatchCondition);
            ConditionParsers.Add(DicomVR.IS, ParseStringValueMatchCondition);

            ConditionParsers.Add(DicomVR.SL, ParseLongValueMatchCondition);
            ConditionParsers.Add(DicomVR.SS, ParseLongValueMatchCondition);
            ConditionParsers.Add(DicomVR.UL, ParseLongValueMatchCondition);
            ConditionParsers.Add(DicomVR.US, ParseLongValueMatchCondition);

            ConditionParsers.Add(DicomVR.FL, ParseDoubleValueMatchCondition);
            ConditionParsers.Add(DicomVR.FD, ParseDoubleValueMatchCondition);
        }

        public static bool TryParseQueryFilterCondition(QueryTag queryTag, string value, out QueryFilterCondition condition)
        {
            condition = null;
            if (ConditionParsers.TryGetValue(queryTag.VR, out ConditionParserFunc conditionParser))
            {
                condition = conditionParser(queryTag, value);
                return true;
            }
            return false;
        }

        private static QueryFilterCondition ParseDateValueMatchCondition(QueryTag queryTag, string value)
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

        private static QueryFilterCondition ParseStringValueMatchCondition(QueryTag queryTag, string value)
        {
            return new StringSingleValueMatchCondition(queryTag, value);
        }

        private static QueryFilterCondition ParseDoubleValueMatchCondition(QueryTag queryTag, string value)
        {
            if (!double.TryParse(value, out double val))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidDoubleValue, value, queryTag.GetName()));
            }

            return new DoubleSingleValueMatchCondition(queryTag, val);
        }

        private static QueryFilterCondition ParseLongValueMatchCondition(QueryTag queryTag, string value)
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
