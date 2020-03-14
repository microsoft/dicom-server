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
            DicomTag.StudyInstanceUID,
            DicomTag.StudyDescription,
            DicomTag.AccessionNumber,
            DicomTag.PatientID,
            DicomTag.PatientName,
        };

        private static readonly HashSet<DicomTag> AllStudyTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> DefaultSeriesTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> AllSeriesTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> DefaultInstancesTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> AllInstancesTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> DefaultStudySeriesTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> AllStudySeriesTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> DefaultStudySeriesInstanceTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> AllStudySeriesInstanceTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> DefaultSeriesInstanceTags = new HashSet<DicomTag>();

        private static readonly HashSet<DicomTag> AllSeriesInstanceTags = new HashSet<DicomTag>();

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
                    levelSpecificTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags));
                    break;
                case QueryResource.StudySeries:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllSeriesTags : DefaultSeriesTags;
                    levelSpecificTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags));
                    break;
                case QueryResource.AllInstances:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllStudySeriesInstanceTags : DefaultStudySeriesInstanceTags;
                    levelSpecificTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags.Union(DefaultInstancesTags)));
                    break;
                case QueryResource.StudyInstances:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllSeriesInstanceTags : DefaultSeriesInstanceTags;
                    levelSpecificTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags.Union(DefaultInstancesTags)));
                    break;
                case QueryResource.StudySeriesInstances:
                    tagsToReturn = queryExpression.IncludeFields.All ? AllInstancesTags : DefaultInstancesTags;
                    levelSpecificTags = new HashSet<DicomTag>(AllStudyTags.Union(AllSeriesTags.Union(DefaultInstancesTags)));
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
