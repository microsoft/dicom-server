// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    /// <summary>
    /// Vaoue parsers
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
                    ParseDate(minDate, dicomTag.DictionaryEntry.Keyword, out DateTime parsedMinDate);
                    ParseDate(maxDate, dicomTag.DictionaryEntry.Keyword, out DateTime parsedMaxDate);

                    return new DateRangeValueMatchCondition(dicomTag, parsedMinDate, parsedMaxDate);
                }
            }

            ParseDate(value, dicomTag.DictionaryEntry.Keyword, out DateTime parsedDate);
            return new DateSingleValueMatchCondition(dicomTag, parsedDate);
        }

        private static DicomQueryFilterCondition ParseStringTagValue(DicomTag dicomTag, string value)
        {
            return new StringSingleValueMatchCondition(dicomTag, value);
        }

        private static void ParseDate(string date, string tagKeyword, out DateTime parsedDate)
        {
            if (!DateTime.TryParseExact(date, DateTagValueFormat, null, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                throw new DicomQueryParseException(string.Format(DicomCoreResource.InvalidDateValue, date, tagKeyword));
            }
        }
    }
}
