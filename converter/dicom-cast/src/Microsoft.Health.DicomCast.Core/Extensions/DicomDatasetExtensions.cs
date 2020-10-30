// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Extensions
{
    public static class DicomDatasetExtensions
    {
        public static FhirDateTime GetDatePropertyIfNotDefaultValue(this DicomDataset dataset, DicomTag dateDicomTag)
        {
            if (dataset.TryGetSingleValue(dateDicomTag, out DateTime dateTagValue) && dateTagValue != DateTime.MinValue)
            {
                return new FhirDateTime(dateTagValue.Year, dateTagValue.Month, dateTagValue.Day);
            }

            return null;
        }

        public static FhirDateTime GetDateTimePropertyIfNotDefaultValue(this DicomDataset dataset, DicomTag dateDicomTag, DicomTag timeDicomTag, TimeSpan utcOffset)
        {
            if (dataset.TryGetSingleValue(dateDicomTag, out DateTime studyDate) && dataset.TryGetSingleValue(timeDicomTag, out DateTime studyTime))
            {
                if (studyDate != DateTime.MinValue || studyTime != DateTime.MinValue)
                {
                    DateTimeOffset studyDateTime = new DateTimeOffset(
                        studyDate.Year,
                        studyDate.Month,
                        studyDate.Day,
                        studyTime.Hour,
                        studyTime.Minute,
                        studyTime.Second,
                        studyTime.Millisecond,
                        utcOffset);

                    return new FhirDateTime(studyDateTime);
                }
            }

            return null;
        }
    }
}
