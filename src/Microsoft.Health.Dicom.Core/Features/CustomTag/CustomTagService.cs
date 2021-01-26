// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
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
        private readonly ICustomTagEntryValidator _customTagEntryValidator;
        private readonly ICustomTagEntryFormalizer _customTagEntryFormalizer;
        private readonly ILogger<CustomTagService> _logger;

        public CustomTagService(ICustomTagStore customTagStore, ICustomTagEntryValidator customTagEntryValidator, ICustomTagEntryFormalizer customTagEntryFormalizer, ILogger<CustomTagService> logger)
        {
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(customTagEntryValidator, nameof(customTagEntryValidator));
            EnsureArg.IsNotNull(customTagEntryFormalizer, nameof(customTagEntryFormalizer));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _customTagStore = customTagStore;
            _customTagEntryValidator = customTagEntryValidator;
            _customTagEntryFormalizer = customTagEntryFormalizer;
            _logger = logger;
        }

        public async Task<AddCustomTagResponse> AddCustomTagAsync(IEnumerable<CustomTagEntry> customTags, CancellationToken cancellationToken = default)
        {
            _customTagEntryValidator.ValidateCustomTags(customTags);

            IEnumerable<CustomTagEntry> result = customTags.Select(item => _customTagEntryFormalizer.Formalize(item));

            await _customTagStore.AddCustomTagsAsync(result, cancellationToken);

            // Current solution is synchronouse, no job uri is generated, so always return emtpy.
            return new AddCustomTagResponse(job: string.Empty);
        }
    }
}
