// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Messages.CustomTag;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    public class CustomTagService : ICustomTagService
    {
        private readonly ICustomTagStore _customTagStore;
        private readonly IReindexJob _reindexJob;
        private readonly ICustomTagEntryValidator _customTagEntryValidator;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly ILogger<CustomTagService> _logger;

        public CustomTagService(ICustomTagStore customTagStore, IReindexJob reindexJob, ICustomTagEntryValidator customTagEntryValidator, IDicomTagParser dicomTagParser, ILogger<CustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(reindexJob, nameof(reindexJob));
            EnsureArg.IsNotNull(customTagEntryValidator, nameof(customTagEntryValidator));
            EnsureArg.IsNotNull(dicomTagParser, nameof(dicomTagParser));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _reindexJob = reindexJob;
            _customTagEntryValidator = customTagEntryValidator;
            _dicomTagParser = dicomTagParser;
            _logger = logger;
        }

        public async Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default)
        {
            // TODO: this synchronous solution is transient, we will finally move onto job framework once it comes out.
            // This transient solution should be able to work on main scnearios.

            // TODO: when moving to job framework, these problems need to be considered
            // 1. What if fail out for lost database connection when adding custom tags, in this case clean up will also not work
            // 2. What if fail out during GetLatestInstanceAsync
            // 3. What if fail out during reindex -- ideally the job frameworks should allow resume failed job
            // 4. What if fail out during update custom tag status

            // Validate input
            _customTagEntryValidator.ValidateCustomTags(customTags);

            List<CustomTagStoreEntry> addedCustomTagStoreEntries = new List<CustomTagStoreEntry>();
            foreach (var customTag in customTags)
            {
                try
                {
                    CustomTagStatus initStatus = CustomTagStatus.Reindexing;

                    // we only support top level dicomtag right now, will support embeded dicomtag soon.
                    DicomTag[] tags;
                    if (!_dicomTagParser.TryParse(customTag.Path, out tags, supportMultiple: false))
                    {
                        // not a valid dicom tag path
                        throw new CustomTagEntryValidationException(
                            string.Format(CultureInfo.InvariantCulture, DicomCoreResource.InvalidCustomTag, customTag));
                    }

                    DicomTag tag = tags[0];
                    string path = tag.GetPath();

                    string vr = customTag.VR;

                    // when VR is not specified for standard tag,
                    if (!tag.IsPrivate && tag.DictionaryEntry != DicomDictionary.UnknownTag)
                    {
                        if (string.IsNullOrWhiteSpace(vr))
                        {
                            vr = tag.GetDefaultVR()?.Code;
                        }
                    }

                    long key = await _customTagStore.AddCustomTagAsync(path, vr, customTag.Level, initStatus);
                    CustomTagStoreEntry storeEntry = new CustomTagStoreEntry(key, path, vr, customTag.Level, initStatus);
                    addedCustomTagStoreEntries.Add(storeEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed to add custom tag {customTag}.", customTag);

                    // clean up
                    foreach (var customTagStoreEntry in addedCustomTagStoreEntries)
                    {
                        await _customTagStore.DeleteCustomTagAsync(customTagStoreEntry.Key);
                    }

                    throw;
                }
            }

            // Current solution won't be able to handle when GetLatestInstanceAsyc fails, when moving to job framework, should solve it.
            long? lastWatermark = await _customTagStore.GetLatestInstanceAsync(cancellationToken);

            // if lastWatermark doesn't exist, means no instance in database
            if (lastWatermark.HasValue)
            {
                // Reindex from latest one to earliest
                // Current solution won't be able to handle when reindex fail in the middle, when moving to job framework, should solve it.
                await _reindexJob.ReindexAsync(addedCustomTagStoreEntries, lastWatermark.Value);
            }

            // Update tag status
            foreach (var customTagStoreEntry in addedCustomTagStoreEntries)
            {
                // Current solution won't be able to handle when update custom tag fail in the middle, when moving to job framework, should solve it.
                await _customTagStore.UpdateCustomTagStatusAsync(customTagStoreEntry.Key, CustomTagStatus.Added);
            }

            // Current solution is synchronouse, no job uri is generated, so always return emtpy.
            return new AddCustomTagResponse(job: string.Empty);
        }
    }
}
