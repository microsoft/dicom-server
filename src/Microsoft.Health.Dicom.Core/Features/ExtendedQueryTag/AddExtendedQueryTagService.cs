// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagService : IAddExtendedQueryTagService
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomOperationsClient _client;
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly int _maxAllowedCount;

        public AddExtendedQueryTagService(
            IExtendedQueryTagStore extendedQueryTagStore,
            IDicomOperationsClient client,
            IExtendedQueryTagEntryValidator extendedQueryTagEntryValidator,
            IOptions<ExtendedQueryTagConfiguration> extendedQueryTagConfiguration)
        {
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(extendedQueryTagEntryValidator, nameof(extendedQueryTagEntryValidator));
            EnsureArg.IsNotNull(extendedQueryTagConfiguration?.Value, nameof(extendedQueryTagConfiguration));

            _extendedQueryTagStore = extendedQueryTagStore;
            _client = client;
            _extendedQueryTagEntryValidator = extendedQueryTagEntryValidator;
            _maxAllowedCount = extendedQueryTagConfiguration.Value.MaxAllowedCount;
        }

        public async Task<AddExtendedQueryTagResponse> AddExtendedQueryTagsAsync(IEnumerable<AddExtendedQueryTagEntry> extendedQueryTags, CancellationToken cancellationToken = default)
        {
            _extendedQueryTagEntryValidator.ValidateExtendedQueryTags(extendedQueryTags);
            List<AddExtendedQueryTagEntry> normalized = extendedQueryTags
                .Select(item => item.Normalize())
                .ToList();

            IReadOnlyList<int> keys = await _extendedQueryTagStore.UpsertExtendedQueryTagsAsync(normalized, _maxAllowedCount, cancellationToken);
            return new AddExtendedQueryTagResponse(await _client.StartQueryTagIndex(keys, cancellationToken));
        }
    }
}
