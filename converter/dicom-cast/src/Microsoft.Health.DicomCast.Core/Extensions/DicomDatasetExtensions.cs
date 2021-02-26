// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Extensions
{
    public static class DicomDatasetExtensions
    {
        public static Date GetDatePropertyIfNotDefaultValue(this DicomDataset dataset, DicomTag dateDicomTag)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            if (dataset.TryGetSingleValue(dateDicomTag, out DateTime dateTagValue) && dateTagValue != DateTime.MinValue)
            {
                var fhirDate = new Date(dateTagValue.Year, dateTagValue.Month, dateTagValue.Day);
                if (Date.IsValidValue(fhirDate.Value))
                {
                    return fhirDate;
                }
            }

            return null;
        }

        public static FhirDateTime GetDateTimePropertyIfNotDefaultValue(this DicomDataset dataset, DicomTag dateDicomTag, DicomTag timeDicomTag, TimeSpan utcOffset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));

            if (dataset.TryGetSingleValue(dateDicomTag, out DateTime studyDate) && dataset.TryGetSingleValue(timeDicomTag, out DateTime studyTime))
            {
                if (studyDate != DateTime.MinValue || studyTime != DateTime.MinValue)
                {
                    var studyDateTime = new DateTimeOffset(
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
