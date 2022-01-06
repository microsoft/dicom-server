// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    internal static class QueryLimit
    {
        public const int MaxQueryResultCount = 200;
        public const int DefaultQueryResultCount = 100;

        private static readonly HashSet<DicomTag> CoreStudyTags = new HashSet<DicomTag>()
        {
            DicomTag.StudyDate,
            DicomTag.StudyInstanceUID,
            DicomTag.StudyDescription,
            DicomTag.AccessionNumber,
            DicomTag.PatientID,
            DicomTag.PatientName,
            DicomTag.ReferringPhysicianName,
            DicomTag.PatientBirthDate,
        };

        private static readonly HashSet<DicomTag> CoreSeriesTags = new HashSet<DicomTag>()
        {
            DicomTag.SeriesInstanceUID,
            DicomTag.Modality,
            DicomTag.PerformedProcedureStepStartDate,
            DicomTag.ManufacturerModelName,
        };

        private static readonly HashSet<DicomTag> CoreInstanceTags = new HashSet<DicomTag>()
        {
            DicomTag.SOPInstanceUID,
        };

        public static readonly HashSet<DicomTag> CoreTags = new HashSet<DicomTag>(
            CoreStudyTags.Union(CoreSeriesTags).Union(CoreInstanceTags));

        public static readonly HashSet<DicomVR> ValidRangeQueryTags = new HashSet<DicomVR>()
        {
            DicomVR.DA,
            DicomVR.DT,
            DicomVR.TM,
        };

        public static readonly IReadOnlyDictionary<QueryResource, ImmutableHashSet<QueryTagLevel>> QueryResourceTypeToQueryLevelsMapping = new Dictionary<QueryResource, ImmutableHashSet<QueryTagLevel>>()
        {
            { QueryResource.AllStudies, ImmutableHashSet.Create(QueryTagLevel.Study) },
            { QueryResource.AllSeries, ImmutableHashSet.Create(QueryTagLevel.Study, QueryTagLevel.Series) },
            { QueryResource.AllInstances, ImmutableHashSet.Create(QueryTagLevel.Study, QueryTagLevel.Series, QueryTagLevel.Instance)  },
            { QueryResource.StudySeries, ImmutableHashSet.Create(QueryTagLevel.Series)},
            { QueryResource.StudyInstances,  ImmutableHashSet.Create(QueryTagLevel.Series, QueryTagLevel.Instance) },
            { QueryResource.StudySeriesInstances,  ImmutableHashSet.Create(QueryTagLevel.Instance) },
        };

        /// <summary>
        /// Get QueryTagLevel of a core tag
        /// </summary>
        /// <param name="coreTag"></param>
        /// <returns></returns>
        public static QueryTagLevel GetQueryTagLevel(DicomTag coreTag)
        {
            EnsureArg.IsNotNull(coreTag, nameof(coreTag));

            if (CoreStudyTags.Contains(coreTag))
            {
                return QueryTagLevel.Study;
            }
            if (CoreSeriesTags.Contains(coreTag))
            {
                return QueryTagLevel.Series;
            }
            if (CoreInstanceTags.Contains(coreTag))
            {
                return QueryTagLevel.Instance;
            }

            Debug.Fail($"{coreTag} is not a core dicom tag");
            return QueryTagLevel.Instance;
        }

        public static bool IsValidRangeQueryTag(QueryTag queryTag)
        {
            EnsureArg.IsNotNull(queryTag, nameof(queryTag));
            return ValidRangeQueryTags.Contains(queryTag.VR);
        }

        public static bool IsValidFuzzyMatchingQueryTag(QueryTag queryTag)
        {
            EnsureArg.IsNotNull(queryTag, nameof(queryTag));
            return queryTag.VR == DicomVR.PN;
        }
    }
}
