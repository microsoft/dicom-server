// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryResponseBuilder
    {
        private static readonly HashSet<DicomTag> DefaultStudyTags = new HashSet<DicomTag>()
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
            DicomTag.SpecificCharacterSet,
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
        };

        private static readonly HashSet<DicomTag> AllInstancesTags = new HashSet<DicomTag>(DefaultInstancesTags);

        private static readonly HashSet<DicomTag> DefaultStudySeriesTags = new HashSet<DicomTag>(DefaultStudyTags.Union(DefaultSeriesTags));

        private static readonly HashSet<DicomTag> AllStudySeriesTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags));

        private static readonly HashSet<DicomTag> DefaultStudySeriesInstanceTags = new HashSet<DicomTag>(DefaultStudyTags.Union(DefaultSeriesTags).Union(DefaultInstancesTags));

        private static readonly HashSet<DicomTag> AllStudySeriesInstanceTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags).Union(AllInstancesTags));

        private static readonly HashSet<DicomTag> DefaultSeriesInstanceTags = new HashSet<DicomTag>(DefaultSeriesTags.Union(DefaultInstancesTags));

        private static readonly HashSet<DicomTag> AllSeriesInstanceTags = new HashSet<DicomTag>(AllSeriesTags.Union(AllInstancesTags));

        private HashSet<DicomTag> _tagsToReturn;

        public QueryResponseBuilder(QueryExpression queryExpression)
        {
            EnsureArg.IsNotNull(queryExpression, nameof(queryExpression));
            EnsureArg.IsFalse(queryExpression.IELevel == ResourceType.Frames, nameof(queryExpression.IELevel));

            Initialize(queryExpression);
        }

        public DicomDataset GenerateResponseDataset(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            dicomDataset.Remove(di => !_tagsToReturn.Any(
                t => t.Group == di.Tag.Group &&
                t.Element == di.Tag.Element));

            return dicomDataset;
        }

        private void Initialize(QueryExpression queryExpression)
        {
            switch (queryExpression.QueryResource)
            {
                case QueryResource.AllStudies:
                    _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllStudyTags : DefaultStudyTags);
                    break;
                case QueryResource.AllSeries:
                    _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllStudySeriesTags : DefaultStudySeriesTags);
                    break;
                case QueryResource.StudySeries:
                    _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllSeriesTags : DefaultSeriesTags);
                    break;
                case QueryResource.AllInstances:
                    _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllStudySeriesInstanceTags : DefaultStudySeriesInstanceTags);
                    break;
                case QueryResource.StudyInstances:
                    _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllSeriesInstanceTags : DefaultSeriesInstanceTags);
                    break;
                case QueryResource.StudySeriesInstances:
                    _tagsToReturn = new HashSet<DicomTag>(queryExpression.IncludeFields.All ? AllInstancesTags : DefaultInstancesTags);
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
                _tagsToReturn.Add(cond.DicomTag);
            }
        }
    }
}
