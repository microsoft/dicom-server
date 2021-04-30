// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public static class QueryLimit
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
        };


        private static readonly HashSet<DicomTag> CoreSeriesTags = new HashSet<DicomTag>()
        {
            DicomTag.SeriesInstanceUID,
            DicomTag.Modality,
            DicomTag.PerformedProcedureStepStartDate,
        };

        private static readonly HashSet<DicomTag> CoreInstanceTags = new HashSet<DicomTag>()
        {
            DicomTag.SOPInstanceUID,
        };

        public static readonly HashSet<DicomTag> CoreTags = new HashSet<DicomTag>(
            CoreStudyTags.Union(CoreSeriesTags).Union(CoreInstanceTags));


        public static readonly IReadOnlyDictionary<QueryResource, ISet<QueryTagLevel>> QueryResourceTypeToQueryLevelsMapping = new Dictionary<QueryResource, ISet<QueryTagLevel>>()
        {
            { QueryResource.AllStudies, new HashSet<QueryTagLevel>(){ QueryTagLevel.Study } },
            { QueryResource.AllSeries, new HashSet<QueryTagLevel>(){ QueryTagLevel.Study, QueryTagLevel.Series } },
            { QueryResource.AllInstances, new HashSet<QueryTagLevel>(){ QueryTagLevel.Study, QueryTagLevel.Series, QueryTagLevel.Instance }  },
            { QueryResource.StudySeries, new HashSet<QueryTagLevel>(){ QueryTagLevel.Series }  },
            { QueryResource.StudyInstances,  new HashSet<QueryTagLevel>(){ QueryTagLevel.Series, QueryTagLevel.Instance } },
            { QueryResource.StudySeriesInstances,  new HashSet<QueryTagLevel>(){  QueryTagLevel.Instance } },
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
            return queryTag.VR == DicomVR.DA;
        }

        public static bool IsValidFuzzyMatchingQueryTag(QueryTag queryTag)
        {
            EnsureArg.IsNotNull(queryTag, nameof(queryTag));
            return queryTag.VR == DicomVR.PN;
        }
    }
}
