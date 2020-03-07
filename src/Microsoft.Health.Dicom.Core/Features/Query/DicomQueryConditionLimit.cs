// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Dicom;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public static class DicomQueryConditionLimit
    {
        public const int MaxQueryResultCount = 200;
        public const int DefaultQueryResultCount = 100;

        public static readonly HashSet<DicomTag> DicomStudyQueryTagsSupported = new HashSet<DicomTag>()
        {
            DicomTag.StudyDate,
            DicomTag.StudyInstanceUID,
            DicomTag.StudyDescription,
            DicomTag.AccessionNumber,
            DicomTag.PatientID,
            DicomTag.PatientName,
        };

        public static readonly HashSet<DicomTag> DicomSeriesQueryTagsSupported = new HashSet<DicomTag>(DicomStudyQueryTagsSupported)
        {
            DicomTag.SeriesInstanceUID,
            DicomTag.Modality,
            DicomTag.PerformedProcedureStepStartDate,
        };

        public static readonly HashSet<DicomTag> DicomInstanceQueryTagsSupported = new HashSet<DicomTag>(DicomSeriesQueryTagsSupported)
        {
            DicomTag.SOPInstanceUID,
        };

        public static bool IsValidRangeQueryTag(DicomTag tag)
        {
            return tag == DicomTag.StudyDate;
        }

        public static bool IsValidFuzzyMatchingQueryTag(DicomTag tag)
        {
            return tag == DicomTag.PatientName;
        }
    }
}
