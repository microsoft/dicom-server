// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Value parsers
    /// </summary>
    public partial class QueryParser
    {
        private static QueryFilterCondition ParseDateTagValue(DicomAttributeId attributeId, string value)
        {
            if (QueryLimit.IsValidRangeQueryTag(attributeId.Tag))
            {
                var splitString = value.Split('-');
                if (splitString.Length == 2)
                {
                    string minDate = splitString[0].Trim();
                    string maxDate = splitString[1].Trim();
                    DateTime parsedMinDate = ParseDate(minDate, attributeId.Tag.DictionaryEntry.Keyword);
                    DateTime parsedMaxDate = ParseDate(maxDate, attributeId.Tag.DictionaryEntry.Keyword);

                    if (parsedMinDate > parsedMaxDate)
                    {
                        throw new QueryParseException(string.Format(
                            DicomCoreResource.InvalidDateRangeValue,
                            value,
                            minDate,
                            maxDate));
                    }

                    return new DateRangeValueMatchCondition(attributeId, parsedMinDate, parsedMaxDate);
                }
            }

            DateTime parsedDate = ParseDate(value, attributeId.Tag.DictionaryEntry.Keyword);
            return new DateSingleValueMatchCondition(attributeId, parsedDate);
        }

        private static QueryFilterCondition ParseStringTagValue(DicomAttributeId attributeId, string value)
        {
            return new StringSingleValueMatchCondition(attributeId, value);
        }

        private static QueryFilterCondition ParseIntTagValue(DicomAttributeId attributeId, string value)
        {
            throw new NotImplementedException();
        }

        private static QueryFilterCondition ParseDecimalTagValue(DicomAttributeId attributeId, string value)
        {
            throw new NotImplementedException();
        }

        private static DateTime ParseDate(string date, string tagKeyword)
        {
            DateTime parsedDate;
            if (!DateTime.TryParseExact(date, DateTagValueFormat, null, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                throw new QueryParseException(string.Format(DicomCoreResource.InvalidDateValue, date, tagKeyword));
            }

            return parsedDate;
        }
    }
}
