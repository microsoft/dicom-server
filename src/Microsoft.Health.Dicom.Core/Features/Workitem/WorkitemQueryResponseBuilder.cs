// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    public static class WorkitemQueryResponseBuilder
    {
        public static DicomDataset GenerateResponseDataset(DicomDataset dicomDataset, BaseQueryExpression queryExpression)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(queryExpression, nameof(queryExpression));

            var tagsToReturn = new HashSet<DicomTag>(QueryLimit.AllRequiredWorkitemTags);

            foreach (DicomTag tag in queryExpression.IncludeFields.DicomTags)
            {
                tagsToReturn.Add(tag);
            }

            foreach (var cond in queryExpression.FilterConditions)
            {
                tagsToReturn.Add(cond.QueryTag.Tag);
            }

            dicomDataset.Remove(di => !tagsToReturn.Any(
                t => t.Group == di.Tag.Group &&
                t.Element == di.Tag.Element));

            return dicomDataset;
        }
    }
}
