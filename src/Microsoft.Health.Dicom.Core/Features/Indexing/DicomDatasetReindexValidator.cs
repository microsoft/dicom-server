// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Validation.Dataset;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    public class DicomDatasetReindexValidator : IDicomDatasetReindexValidator
    {
        private readonly IDicomElementMinimumValidator _minimumValidator;

        public DicomDatasetReindexValidator(IDicomElementMinimumValidator minimumValidator)
        {
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
        }
        public IReadOnlyCollection<QueryTag> Validate(DicomDataset dicomDataset, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            HashSet<DicomTag> invalidTags = new HashSet<DicomTag>();
            var validation = new DatasetQueryTagsValidation(queryTags, _minimumValidator, (queryTag, exception) =>
           {
               invalidTags.Add(queryTag.Tag);
               // TODO: log failure

               // continue validating next tag.
               return false;
           });

            if (invalidTags.Count == 0)
            {
                return queryTags;
            }

            return queryTags.Where(x => !invalidTags.Contains(x.Tag)).ToList();
        }

    }
}
