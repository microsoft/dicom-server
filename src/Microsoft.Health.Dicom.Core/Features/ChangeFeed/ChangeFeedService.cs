// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Models;

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

    public async Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedAsync(TimeRange range, long offset, int limit, ChangeFeedOrder order, bool includeMetadata, CancellationToken cancellationToken = default)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit));

        IReadOnlyList<ChangeFeedEntry> changeFeedEntries = await _changeFeedStore.GetChangeFeedAsync(range, offset, limit, order, cancellationToken);

        if (includeMetadata)
        {
            await Parallel.ForEachAsync(
                changeFeedEntries.Where(x => x.CurrentVersion.HasValue),
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
                },
                async (entry, t) =>
                {
                    entry.IncludeMetadata = true;
                    entry.Metadata = await _metadataStore.GetInstanceMetadataAsync(entry.CurrentVersion.GetValueOrDefault(), t);
                });
        }

        return changeFeedEntries;
    }

    public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(ChangeFeedOrder order, bool includeMetadata, CancellationToken cancellationToken = default)
    {
        ChangeFeedEntry result = await _changeFeedStore.GetChangeFeedLatestAsync(order, cancellationToken);

        if (result == null)
            return null;

        if (includeMetadata && result.CurrentVersion.HasValue)
        {
            result.IncludeMetadata = true;
            result.Metadata = await _metadataStore.GetInstanceMetadataAsync(result.CurrentVersion.Value, cancellationToken);
        }

        return result;
    }
}
