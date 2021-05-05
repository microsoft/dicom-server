// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagService : IAddExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly IReindexService _reindexService;

        public AddExtendedQueryTagService(IExtendedQueryTagStore extendedQueryTagStore, IExtendedQueryTagEntryValidator extendedQueryTagEntryValidator, IReindexService reindexService)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(extendedQueryTagEntryValidator, nameof(extendedQueryTagEntryValidator));
            EnsureArg.IsNotNull(reindexService, nameof(reindexService));

            _extendedQueryTagStore = extendedQueryTagStore;
            _extendedQueryTagEntryValidator = extendedQueryTagEntryValidator;
            _reindexService = reindexService;
        }

        public async Task<AddExtendedQueryTagResponse> AddExtendedQueryTagAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken)
        {
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(extendedQueryTags);

            IEnumerable<AddExtendedQueryTagEntry> result = extendedQueryTags.Select(item => item.Normalize());

            var entries = await _extendedQueryTagStore.AddExtendedQueryTagsAsync(result, cancellationToken);

            // start job

            await _reindexService.StartNewReindexJob(entries, cancellationToken);
            // Current solution is synchronous, no job uri is generated, so always return blank response.
            return new AddExtendedQueryTagResponse();
        }
    }
}
