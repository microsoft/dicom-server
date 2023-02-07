// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query;

internal static class QueryLimit
{
    public const int MaxQueryResultCount = 200;
    public const int DefaultQueryResultCount = 100;

    private static readonly HashSet<DicomTag> StudyFilterTags = new HashSet<DicomTag>()
    {
        DicomTag.StudyDate,
        DicomTag.StudyInstanceUID,
        DicomTag.StudyDescription,
        DicomTag.AccessionNumber,
        DicomTag.PatientID,
        DicomTag.PatientName,
        DicomTag.ReferringPhysicianName,
        DicomTag.PatientBirthDate,
        DicomTag.ModalitiesInStudy,
    };

    private static readonly HashSet<DicomTag> SeriesFilterTags = new HashSet<DicomTag>()
    {
        DicomTag.SeriesInstanceUID,
        DicomTag.Modality,
        DicomTag.PerformedProcedureStepStartDate,
        DicomTag.ManufacturerModelName,
    };

    private static readonly HashSet<DicomTag> InstanceFilterTags = new HashSet<DicomTag>()
    {
        DicomTag.SOPInstanceUID,
    };

    private static readonly HashSet<DicomTag> WorkitemQueryParseTags = new HashSet<DicomTag>()
    {
        DicomTag.RequestedProcedureID,
        DicomTag.CodeValue
    };

    private static readonly HashSet<DicomTag> StudyResultComputedTags = new HashSet<DicomTag>()
    {
        DicomTag.ModalitiesInStudy,
        DicomTag.NumberOfStudyRelatedInstances
    };

    private static readonly HashSet<DicomTag> SeriesResultComputedTags = new HashSet<DicomTag>()
    {
        DicomTag.NumberOfSeriesRelatedInstances
    };

    public static readonly HashSet<DicomTag> CoreFilterTags = new HashSet<DicomTag>(
        StudyFilterTags.Union(SeriesFilterTags).Union(InstanceFilterTags));

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

        if (StudyFilterTags.Contains(coreTag))
        {
            return QueryTagLevel.Study;
        }
        if (SeriesFilterTags.Contains(coreTag))
        {
            return QueryTagLevel.Series;
        }
        if (InstanceFilterTags.Contains(coreTag))
        {
            return QueryTagLevel.Instance;
        }
        if (WorkitemQueryParseTags.Contains(coreTag))
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

    public static bool ContainsComputedTag(ResourceType queryTagLevel, IReadOnlyCollection<DicomTag> tags)
    {
        return queryTagLevel switch
        {
            ResourceType.Study => tags.Any(t => StudyResultComputedTags.Contains(t)),
            ResourceType.Series => tags.Any(t => SeriesResultComputedTags.Contains(t)),
            _ => false
        };
    }

    public static bool IsStudyToSeriesTag(DicomTag tag)
    {
        return tag == DicomTag.ModalitiesInStudy;
    }
}
