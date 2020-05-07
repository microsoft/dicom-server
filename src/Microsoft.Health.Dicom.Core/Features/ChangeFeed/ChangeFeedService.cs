// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
{
    public class ChangeFeedService : IChangeFeedService
    {
        private readonly IChangeFeedStore _changeFeedStore;
        private readonly IMetadataStore _metadataStore;

        public ChangeFeedService(IChangeFeedStore changeFeedStore, IMetadataStore metadataStore)
        {
            EnsureArg.IsNotNull(changeFeedStore, nameof(changeFeedStore));
            EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));

            _changeFeedStore = changeFeedStore;
            _metadataStore = metadataStore;
        }

        public async Task<IReadOnlyCollection<ChangeFeedEntry>> GetChangeFeedAsync(int offset, int limit, bool includeMetadata, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ChangeFeedEntry> changeFeedEntries = await _changeFeedStore.GetChangeFeedAsync(offset, limit, cancellationToken);

            if (!includeMetadata)
            {
                return changeFeedEntries;
            }

            foreach (ChangeFeedEntry entry in changeFeedEntries)
            {
                await PopulateMetadata(entry, cancellationToken);
            }

            return changeFeedEntries;
        }

        public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(bool includeMetadata, CancellationToken cancellationToken = default)
        {
            var result = await _changeFeedStore.GetChangeFeedLatestAsync(cancellationToken);

            if (result == null)
            {
                return null;
            }

            if (includeMetadata)
            {
                await PopulateMetadata(result, cancellationToken);
            }

            return result;
        }

        private async Task PopulateMetadata(ChangeFeedEntry entry, CancellationToken cancellationToken)
        {
            if (entry.State == ChangeFeedState.Deleted || entry.CurrentVersion == null)
            {
                return;
            }

            var identifier = new VersionedInstanceIdentifier(entry.StudyInstanceUid, entry.SeriesInstanceUid, entry.SopInstanceUid, entry.CurrentVersion.Value);
            entry.Metadata = await _metadataStore.GetInstanceMetadataAsync(identifier, cancellationToken);
            entry.IncludeMetadata = true;
        }
    }
}
