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
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query;

internal class QueryResponseBuilder
{
    internal static readonly ImmutableHashSet<DicomTag> DefaultStudyTags = ImmutableHashSet.CreateRange(new DicomTag[]
    {
        DicomTag.SpecificCharacterSet,
        DicomTag.StudyDate,
        DicomTag.StudyTime,
        DicomTag.AccessionNumber,
        DicomTag.InstanceAvailability,
        DicomTag.ReferringPhysicianName,
        DicomTag.TimezoneOffsetFromUTC,
        DicomTag.PatientName,
        DicomTag.PatientID,
        DicomTag.PatientBirthDate,
        DicomTag.PatientSex,
        DicomTag.StudyInstanceUID,
        DicomTag.StudyID,
    });

    internal static readonly ImmutableHashSet<DicomTag> V2DefaultStudyTags = ImmutableHashSet.CreateRange(new DicomTag[]
    {
        DicomTag.StudyInstanceUID,
        DicomTag.StudyDate,
        DicomTag.StudyDescription,
        DicomTag.AccessionNumber,
        DicomTag.ReferringPhysicianName,
        DicomTag.PatientName,
        DicomTag.PatientID,
        DicomTag.PatientBirthDate
    });

    internal static readonly ImmutableHashSet<DicomTag> AllStudyTags = DefaultStudyTags.Union(new DicomTag[]
    {
        DicomTag.StudyDescription,
        DicomTag.AnatomicRegionsInStudyCodeSequence,
        DicomTag.ProcedureCodeSequence,
        DicomTag.NameOfPhysiciansReadingStudy,
        DicomTag.AdmittingDiagnosesDescription,
        DicomTag.ReferencedStudySequence,
        DicomTag.PatientAge,
        DicomTag.PatientSize,
        DicomTag.PatientWeight,
        DicomTag.Occupation,
        DicomTag.AdditionalPatientHistory,
    });

    internal static readonly ImmutableHashSet<DicomTag> DefaultSeriesTags = ImmutableHashSet.CreateRange(new DicomTag[]
    {
        DicomTag.SpecificCharacterSet,
        DicomTag.Modality,
        DicomTag.TimezoneOffsetFromUTC,
        DicomTag.SeriesDescription,
        DicomTag.SeriesInstanceUID,
        DicomTag.PerformedProcedureStepStartDate,
        DicomTag.PerformedProcedureStepStartTime,
        DicomTag.RequestAttributesSequence,
    });


    internal static readonly ImmutableHashSet<DicomTag> V2DefaultSeriesTags = ImmutableHashSet.CreateRange(new DicomTag[]
    {
        DicomTag.SeriesInstanceUID,
        DicomTag.Modality,
        DicomTag.PerformedProcedureStepStartDate,
        DicomTag.ManufacturerModelName
    });

    internal static readonly ImmutableHashSet<DicomTag> AllSeriesTags = DefaultSeriesTags.Union(new DicomTag[]
    {
        DicomTag.SeriesNumber,
        DicomTag.Laterality,
        DicomTag.SeriesDate,
        DicomTag.SeriesTime,
    });

    internal static readonly ImmutableHashSet<DicomTag> DefaultInstancesTags = ImmutableHashSet.CreateRange(new DicomTag[]
    {
        DicomTag.SpecificCharacterSet,
        DicomTag.SOPClassUID,
        DicomTag.SOPInstanceUID,
        DicomTag.InstanceAvailability,
        DicomTag.TimezoneOffsetFromUTC,
        DicomTag.InstanceNumber,
        DicomTag.Rows,
        DicomTag.Columns,
        DicomTag.BitsAllocated,
        DicomTag.NumberOfFrames,
    });

    internal static readonly ImmutableHashSet<DicomTag> V2DefaultInstancesTags = ImmutableHashSet.CreateRange(new DicomTag[]
    {
        DicomTag.SOPInstanceUID
    });

    private static readonly ImmutableHashSet<DicomTag> AllInstancesTags = DefaultInstancesTags;

    private HashSet<DicomTag> _tagsToReturn;

    public QueryResponseBuilder(QueryExpression queryExpression, bool useNewDefaults = false)
    {
        EnsureArg.IsNotNull(queryExpression, nameof(queryExpression));
        EnsureArg.IsFalse(queryExpression.IELevel == ResourceType.Frames, nameof(queryExpression.IELevel));

        Initialize(queryExpression, useNewDefaults);
    }

    public DicomDataset GenerateResponseDataset(DicomDataset dicomDataset)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        dicomDataset.Remove(di => !_tagsToReturn.Any(
            t => t.Group == di.Tag.Group &&
            t.Element == di.Tag.Element));

        return dicomDataset;
    }

    public IReadOnlyCollection<DicomTag> ReturnTags => _tagsToReturn;

    // If the target resource is All Series, then Study level attributes are also returned.
    // If the target resource is All Instances, then Study and Series level attributes are also returned.
    // If the target resource is Study's Instances, then Series level attributes are also returned.
    private void Initialize(QueryExpression queryExpression, bool useNewDefaults)
    {
        switch (queryExpression.QueryResource)
        {
            case QueryResource.AllStudies:
                _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllStudyTags : useNewDefaults ? V2DefaultStudyTags : DefaultStudyTags);
                break;
            case QueryResource.AllSeries:
                _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllStudyTags.Union(AllSeriesTags) : useNewDefaults ? V2DefaultStudyTags.Union(V2DefaultSeriesTags) : DefaultStudyTags.Union(DefaultSeriesTags));
                break;
            case QueryResource.AllInstances:
                _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllStudyTags.Union(AllSeriesTags).Union(AllInstancesTags) : useNewDefaults ? V2DefaultStudyTags.Union(V2DefaultSeriesTags).Union(V2DefaultInstancesTags) : DefaultStudyTags.Union(DefaultSeriesTags).Union(DefaultInstancesTags));
                break;
            case QueryResource.StudySeries:
                _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllSeriesTags : useNewDefaults ? V2DefaultSeriesTags : DefaultSeriesTags);
                break;
            case QueryResource.StudyInstances:
                _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllSeriesTags.Union(AllInstancesTags) : useNewDefaults ? V2DefaultSeriesTags.Union(V2DefaultInstancesTags) : DefaultSeriesTags.Union(DefaultInstancesTags));
                break;
            case QueryResource.StudySeriesInstances:
                _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllInstancesTags : useNewDefaults ? V2DefaultInstancesTags : DefaultInstancesTags);
                break;
            default:
                Debug.Fail("A newly added queryResource is not implemeted here");
                break;
        }

        foreach (DicomTag tag in queryExpression.IncludeFields.DicomTags)
        {
            // we will allow any valid include tag. This will allow customers to get any extended query tags in resposne.
            _tagsToReturn.Add(tag);
        }

        foreach (var cond in queryExpression.FilterConditions)
        {
            _tagsToReturn.Add(cond.QueryTag.Tag);
        }
    }
}
