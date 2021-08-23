// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
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
        private readonly ILogger<ReindexDatasetValidator> _logger;
        private readonly int _maxErrorMessageLength;
        public ReindexDatasetValidator(
            IElementMinimumValidator minimumValidator,
            IExtendedQueryTagErrorStore extendedQueryTagErrorStore,
            IOptions<StoreConfiguration> storeConfig,
            ILogger<ReindexDatasetValidator> logger)
        {
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _extendedQueryTagErrorStore = EnsureArg.IsNotNull(extendedQueryTagErrorStore, nameof(extendedQueryTagErrorStore));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
            _maxErrorMessageLength = EnsureArg.IsNotNull(storeConfig?.Value, nameof(storeConfig)).MaxErrorMessageLength;
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
                    if (e.Message?.Length > _maxErrorMessageLength)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageExceedsMaxLength, e.Message.Length, _maxErrorMessageLength, e.Message);
                        Debug.Fail(message);
                        _logger.LogWarning(message);
                    }

                    _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                        queryTag.ExtendedQueryTagStoreEntry.Key,
                        e.Message?.Truncate(_maxErrorMessageLength),
                        watermark);
                }
            }
            return validTags;
        }
    }
}
