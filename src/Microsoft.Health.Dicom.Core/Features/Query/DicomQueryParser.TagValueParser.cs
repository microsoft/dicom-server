// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Value parsers
    /// </summary>
    public partial class DicomQueryParser
    {
        private static DicomQueryFilterCondition ParseDateTagValue(DicomTag dicomTag, string value)
        {
            if (DicomQueryLimit.IsValidRangeQueryTag(dicomTag))
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
                        throw new DicomQueryParseException(string.Format(
                            DicomCoreResource.InvalidDateRangeValue,
                            value,
                            minDate,
                            maxDate));
                    }

                    return new DateRangeValueMatchCondition(dicomTag, parsedMinDate, parsedMaxDate);
                }
            }

            DateTime parsedDate = ParseDate(value, dicomTag.DictionaryEntry.Keyword);
            return new DateSingleValueMatchCondition(dicomTag, parsedDate);
        }

        private static DicomQueryFilterCondition ParseStringTagValue(DicomTag dicomTag, string value)
        {
            return new StringSingleValueMatchCondition(dicomTag, value);
        }

        private static DateTime ParseDate(string date, string tagKeyword)
        {
            DateTime parsedDate;
            if (!DateTime.TryParseExact(date, DateTagValueFormat, null, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                throw new DicomQueryParseException(string.Format(DicomCoreResource.InvalidDateValue, date, tagKeyword));
            }

            return parsedDate;
        }
    }
}
