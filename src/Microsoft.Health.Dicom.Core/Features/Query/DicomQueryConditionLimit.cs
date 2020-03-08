// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using Dicom;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public static class DicomQueryConditionLimit
    {
        public const int MaxQueryResultCount = 200;
        public const int DefaultQueryResultCount = 100;

        public static readonly HashSet<DicomTag> AllStudiesTags = new HashSet<DicomTag>()
        {
            DicomTag.StudyDate,
            DicomTag.StudyInstanceUID,
            DicomTag.StudyDescription,
            DicomTag.AccessionNumber,
            DicomTag.PatientID,
            DicomTag.PatientName,
        };

        public static readonly HashSet<DicomTag> StudySeriesTags = new HashSet<DicomTag>()
        {
            DicomTag.SeriesInstanceUID,
            DicomTag.Modality,
            DicomTag.PerformedProcedureStepStartDate,
        };

        public static readonly HashSet<DicomTag> StudySeriesInstancesTags = new HashSet<DicomTag>()
        {
            DicomTag.SOPInstanceUID,
        };

        public static readonly HashSet<DicomTag> StudyInstancesTags = new HashSet<DicomTag>(
            StudySeriesTags.Union(StudySeriesInstancesTags));

        public static readonly HashSet<DicomTag> AllSeriesTags = new HashSet<DicomTag>(
            AllStudiesTags.Union(StudySeriesTags));

        public static readonly HashSet<DicomTag> AllInstancesTags = new HashSet<DicomTag>(
            AllStudiesTags.Union(StudySeriesTags).Union(StudySeriesInstancesTags));

        public static readonly Dictionary<QueryResourceType, HashSet<DicomTag>> QueryResourceTypeToTagsMapping = new Dictionary<QueryResourceType, HashSet<DicomTag>>()
        {
            { QueryResourceType.AllStudies, AllStudiesTags },
            { QueryResourceType.AllSeries, AllSeriesTags },
            { QueryResourceType.AllInstances, AllInstancesTags },
            { QueryResourceType.StudySeries, StudySeriesTags },
            { QueryResourceType.StudyInstances, StudyInstancesTags },
            { QueryResourceType.StudySeriesInstances, StudySeriesInstancesTags },
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
