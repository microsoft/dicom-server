// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed;

public class ChangeFeedService : IChangeFeedService
{
    private readonly IChangeFeedStore _changeFeedStore;
    private readonly IMetadataStore _metadataStore;
    private readonly RetrieveConfiguration _options;

    public ChangeFeedService(
        IChangeFeedStore changeFeedStore,
        IMetadataStore metadataStore,
        IOptions<RetrieveConfiguration> options)
    {
        _changeFeedStore = EnsureArg.IsNotNull(changeFeedStore, nameof(changeFeedStore));
        _metadataStore = EnsureArg.IsNotNull(metadataStore, nameof(metadataStore));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(metadataStore));
    }

    public async Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedAsync(DateTimeOffsetRange range, long offset, int limit, bool includeMetadata, ChangeFeedOrder order, CancellationToken cancellationToken = default)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit));

        IReadOnlyList<ChangeFeedEntry> changeFeedEntries = await _changeFeedStore.GetChangeFeedAsync(range, offset, limit, order, cancellationToken);

        if (includeMetadata)
        {
            await Parallel.ForEachAsync(
                changeFeedEntries,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                },
                PopulateMetadata);
        }

        return changeFeedEntries;
    }

    public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(bool includeMetadata, ChangeFeedOrder order, CancellationToken cancellationToken = default)
    {
        ChangeFeedEntry result = await _changeFeedStore.GetChangeFeedLatestAsync(order, cancellationToken);

        if (result == null)
            return null;

        if (includeMetadata)
            await PopulateMetadata(result, cancellationToken);

        return result;
    }

    private async ValueTask PopulateMetadata(ChangeFeedEntry entry, CancellationToken cancellationToken)
    {
        if (entry.CurrentVersion == null)
            return;

        var identifier = new VersionedInstanceIdentifier(entry.StudyInstanceUid, entry.SeriesInstanceUid, entry.SopInstanceUid, entry.CurrentVersion.Value);
        entry.Metadata = await _metadataStore.GetInstanceMetadataAsync(identifier.Version, cancellationToken);
        entry.IncludeMetadata = true;
    }
}
