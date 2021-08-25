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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    public class ReindexDatasetValidator : IReindexDatasetValidator
    {
        /// <summary>
        /// Max length of error message.
        /// </summary>
        /// <remarks>This limitation is from ExtendedQueryTagErrorStore.</remarks>
        public const int MaxErrorMessageLength = 200;

        private readonly IElementMinimumValidator _minimumValidator;
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly ILogger<ReindexDatasetValidator> _logger;
        public ReindexDatasetValidator(
            IElementMinimumValidator minimumValidator,
            IExtendedQueryTagErrorStore extendedQueryTagErrorStore,
            ILogger<ReindexDatasetValidator> logger)
        {
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _extendedQueryTagErrorStore = EnsureArg.IsNotNull(extendedQueryTagErrorStore, nameof(extendedQueryTagErrorStore));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
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
                    string message = e.Message ?? string.Empty;
                    if (message.Length > MaxErrorMessageLength)
                    {
                        string warning = string.Format(CultureInfo.InvariantCulture,
                            DicomCoreResource.ErrorMessageExceedsMaxLength, message.Length, MaxErrorMessageLength, message);
                        Debug.Fail(warning);
                        _logger.LogWarning(warning);
                    }

                    _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                        queryTag.ExtendedQueryTagStoreEntry.Key,
                        message.Truncate(MaxErrorMessageLength),
                        watermark);
                }
            }
            return validTags;
        }
    }
}
