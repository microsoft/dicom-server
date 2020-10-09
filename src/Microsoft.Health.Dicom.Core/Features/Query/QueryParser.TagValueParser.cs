// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Value parsers
    /// </summary>
    public partial class QueryParser
    {
        private static QueryFilterCondition ParseDateTagValue(DicomTag dicomTag, string value)
        {
            if (QueryLimit.IsValidRangeQueryTag(dicomTag))
            {
                var splitString = value.Split('-');
                if (splitString.Length == 2)
                {
                    string minDate = splitString[0].Trim();
                    string maxDate = splitString[1].Trim();
                    DateTime parsedMinDate = ParseDate(minDate, dicomTag.DictionaryEntry.Keyword);
                    DateTime parsedMaxDate = ParseDate(maxDate, dicomTag.DictionaryEntry.Keyword);

                    if (parsedMinDate > parsedMaxDate)
                    {
                        throw new QueryParseException(string.Format(
                            DicomCoreResource.InvalidDateRangeValue,
                            value,
                            minDate,
                            maxDate));
                    }

                    return new DateRangeValueMatchCondition(new DicomAttributeId(dicomTag), parsedMinDate, parsedMaxDate);
                }
            }

            DateTime parsedDate = ParseDate(value, dicomTag.DictionaryEntry.Keyword);
            return new DateSingleValueMatchCondition(new DicomAttributeId(dicomTag), parsedDate);
        }

        private static QueryFilterCondition ParseStringTagValue(DicomTag dicomTag, string value)
        {
            return new StringSingleValueMatchCondition(new DicomAttributeId(dicomTag), value);
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
