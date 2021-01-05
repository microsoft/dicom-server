// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagService : ICustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IReindexJob _reindexJob;
        private readonly ICustomTagEntryValidator _customTagEntryValidator;
        private readonly ILogger<CustomTagService> _logger;

        public CustomTagService(ICustomTagStore customTagStore, IReindexJob reindexJob, ICustomTagEntryValidator customTagEntryValidator, ILogger<CustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(reindexJob, nameof(reindexJob));
            EnsureArg.IsNotNull(customTagEntryValidator, nameof(customTagEntryValidator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _reindexJob = reindexJob;
            _customTagEntryValidator = customTagEntryValidator;
            _logger = logger;
        }

        public async Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default)
        {
            // Validate input
            await _customTagEntryValidator.ValidateCustomTagsAsync(customTags, cancellationToken);

            HashSet<long> addedTagKeys = new HashSet<long>();
            foreach (var tag in customTags)
            {
                try
                {
                    long key = await _customTagStore.AddCustomTagAsync(tag.Path, tag.VR, tag.Level, CustomTagStatus.Reindexing);
                    addedTagKeys.Add(key);
                    tag.Key = key;
                    tag.Status = CustomTagStatus.Reindexing;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed to add custom tag {tag}.", tag);

                    // clean up
                    foreach (var tagkey in addedTagKeys)
                    {
                        await _customTagStore.DeleteCustomTagAsync(tagkey);
                    }

                    throw;
                }
            }

            long? lastWatermark = await _customTagStore.GetLatestInstanceAsync(cancellationToken);

            // if lastWatermark doesn't exist, means no instance in database
            if (lastWatermark.HasValue)
            {
                // Reindex from latest one to earliest
                await _reindexJob.ReindexAsync(customTags, lastWatermark.Value);
            }

            // Update tag status
            foreach (var tag in customTags)
            {
                await _customTagStore.UpdateCustomTagStatusAsync(tag.Key, CustomTagStatus.Added);
                tag.Status = CustomTagStatus.Added;
            }

            return new AddCustomTagResponse(customTags, string.Empty);
        }
    }
}
