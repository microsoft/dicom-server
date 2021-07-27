// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;

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

            List<QueryTag> validTags = new List<QueryTag>();
            foreach (QueryTag queryTag in queryTags)
            {
                DicomElement dicomElement = dicomDataset.GetDicomItem<DicomElement>(queryTag.Tag);

                if (dicomElement != null)
                {
                    if (dicomElement.ValueRepresentation != queryTag.VR)
                    {
                        // TODO: log error in error store
                        continue;
                    }

                    try
                    {
                        _minimumValidator.Validate(dicomElement);
                    }
                    catch (DicomElementValidationException)
                    {
                        // TODO: log error in error store
                        continue;
                    }

                    validTags.Add(queryTag);
                }
            }

            return validTags;
        }
    }
}
