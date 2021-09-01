// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task<IReadOnlyCollection<QueryTag>> ValidateAsync(DicomDataset dataset, long watermark, IReadOnlyCollection<QueryTag> queryTags, CancellationToken cancellationToken)
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
                catch (ElementValidationException ex)
                {
                    if (queryTag.IsExtendedQueryTag)
                    {
                        await _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(queryTag.ExtendedQueryTagStoreEntry.Key, ex.ErrorCode, watermark, cancellationToken);
                    }
                }
            }
            return validTags;
        }
    }
}
