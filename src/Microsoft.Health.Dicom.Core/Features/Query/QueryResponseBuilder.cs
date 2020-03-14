// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public static class QueryResponseBuilder
    {
        private static readonly HashSet<DicomTag> DefaultStudyTags = new HashSet<DicomTag>()
        {
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
        };

        private static readonly HashSet<DicomTag> AllStudyTags = new HashSet<DicomTag>(DefaultStudyTags)
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
        };

        private static readonly HashSet<DicomTag> DefaultSeriesTags = new HashSet<DicomTag>()
        {
            DicomTag.Modality,
            DicomTag.TimezoneOffsetFromUTC,
            DicomTag.SeriesDescription,
            DicomTag.SeriesInstanceUID,
            DicomTag.PerformedProcedureStepStartDate,
            DicomTag.PerformedProcedureStepStartTime,
            DicomTag.RequestAttributesSequence,
        };

        private static readonly HashSet<DicomTag> AllSeriesTags = new HashSet<DicomTag>(DefaultSeriesTags)
        {
            DicomTag.SeriesNumber,
            DicomTag.Laterality,
            DicomTag.SeriesDate,
            DicomTag.SeriesTime,
        };

        private static readonly HashSet<DicomTag> DefaultInstancesTags = new HashSet<DicomTag>()
        {
            DicomTag.SOPClassUID,
            DicomTag.SOPInstanceUID,
            DicomTag.InstanceAvailability,
            DicomTag.TimezoneOffsetFromUTC,
            DicomTag.InstanceNumber,
            DicomTag.Rows,
            DicomTag.Columns,
            DicomTag.BitsAllocated,
            DicomTag.NumberOfFrames,
        };

        private static readonly HashSet<DicomTag> AllInstancesTags = new HashSet<DicomTag>(DefaultInstancesTags);

        private static readonly HashSet<DicomTag> DefaultStudySeriesTags = new HashSet<DicomTag>(DefaultStudyTags.Union(DefaultSeriesTags));

        private static readonly HashSet<DicomTag> AllStudySeriesTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags));

        private static readonly HashSet<DicomTag> DefaultStudySeriesInstanceTags = new HashSet<DicomTag>(DefaultStudyTags.Union(DefaultSeriesTags).Union(DefaultInstancesTags));

        private static readonly HashSet<DicomTag> AllStudySeriesInstanceTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags).Union(AllInstancesTags));

        private static readonly HashSet<DicomTag> DefaultSeriesInstanceTags = new HashSet<DicomTag>(DefaultSeriesTags.Union(DefaultInstancesTags));

        private static readonly HashSet<DicomTag> AllSeriesInstanceTags = new HashSet<DicomTag>(AllSeriesTags.Union(AllInstancesTags));

        public static DicomDataset GenerateResponseDataset(DicomDataset dicomDataset, DicomQueryExpression queryExpression)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(queryExpression, nameof(queryExpression));
            EnsureArg.IsFalse(queryExpression.IELevel == ResourceType.Frames, nameof(queryExpression.IELevel));
            HashSet<DicomTag> levelSpecificTags = null;
            HashSet<DicomTag> tagsToReturn = null;
            switch (queryExpression.QueryResource)
            {
                case QueryResource.AllStudies:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllStudyTags : DefaultStudyTags;
                    levelSpecificTags = AllStudyTags;
                    break;
                case QueryResource.AllSeries:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllStudySeriesTags : DefaultStudySeriesTags;
                    levelSpecificTags = AllStudySeriesTags;
                    break;
                case QueryResource.StudySeries:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllSeriesTags : DefaultSeriesTags;
                    levelSpecificTags = AllStudySeriesTags;
                    break;
                case QueryResource.AllInstances:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllStudySeriesInstanceTags : DefaultStudySeriesInstanceTags;
                    levelSpecificTags = AllStudySeriesInstanceTags;
                    break;
                case QueryResource.StudyInstances:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllSeriesInstanceTags : DefaultSeriesInstanceTags;
                    levelSpecificTags = AllStudySeriesInstanceTags;
                    break;
                case QueryResource.StudySeriesInstances:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllInstancesTags : DefaultInstancesTags;
                    levelSpecificTags = AllStudySeriesInstanceTags;
                    break;
            }

            foreach (DicomTag tag in queryExpression.IncludeFields.DicomTags)
            {
                if (levelSpecificTags.Contains(tag))
                {
                    tagsToReturn.Add(tag);
                }
            }

            foreach (var cond in queryExpression.FilterConditions)
            {
                tagsToReturn.Add(cond.DicomTag);
            }

            dicomDataset.Remove((di) =>
                                {
                                    return !tagsToReturn.Contains(di.Tag);
                                });

            return dicomDataset;
        }
    }
}
