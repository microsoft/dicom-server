// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    public class ReindexDatasetValidator : IReindexDatasetValidator
    {
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;

        public ReindexDatasetValidator(IElementMinimumValidator minimumValidator, IExtendedQueryTagErrorStore extendedQueryTagErrorStore)
        {
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _extendedQueryTagErrorStore = EnsureArg.IsNotNull(extendedQueryTagErrorStore, nameof(extendedQueryTagErrorStore));
        }

        public IReadOnlyCollection<QueryTag> Validate(DicomDataset dataset, long watermark, IReadOnlyCollection<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            List<QueryTag> validTags = new List<QueryTag>();
            foreach (var queryTag in queryTags)
            {
                try
                {
                    dataset.ValidateQueryTag(queryTag, _minimumValidator);
                    validTags.Add(queryTag);
                }
                catch (DicomElementValidationException e)
                {
                    _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                        queryTag.ExtendedQueryTagStoreEntry.Key,
                        e.Message,
                        watermark);
                }
            }
            return validTags;
        }
    }
}
