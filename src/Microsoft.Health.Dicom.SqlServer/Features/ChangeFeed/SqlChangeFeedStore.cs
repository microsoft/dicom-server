// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;

internal class SqlChangeFeedStore : IChangeFeedStore
{
    private readonly VersionedCache<ISqlChangeFeedStore> _cache;

    public SqlChangeFeedStore(VersionedCache<ISqlChangeFeedStore> cache)
        => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

    public async Task<IReadOnlyList<ChangeFeedEntry>> GetChangeFeedAsync(TimeRange range, long offset, int limit, ChangeFeedOrder order, CancellationToken cancellationToken = default)
    {
        ISqlChangeFeedStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetChangeFeedAsync(range, offset, limit, order, cancellationToken);
    }

    public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(ChangeFeedOrder order, CancellationToken cancellationToken = default)
    {
        ISqlChangeFeedStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetChangeFeedLatestAsync(order, cancellationToken);
    }
}
