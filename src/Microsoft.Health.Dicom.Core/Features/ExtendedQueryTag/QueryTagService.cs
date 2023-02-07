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
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Features.Common;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

public sealed class QueryTagService : IQueryTagService, IDisposable
{
    private readonly IExtendedQueryTagStore _extendedQueryTagStore;
    private readonly AsyncCache<IReadOnlyCollection<QueryTag>> _queryTagCache;

    public QueryTagService(IExtendedQueryTagStore extendedQueryTagStore)
    {
        _extendedQueryTagStore = EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
        _queryTagCache = new AsyncCache<IReadOnlyCollection<QueryTag>>(ResolveQueryTagsAsync);
    }

    public static IReadOnlyList<QueryTag> CoreQueryTags { get; } = QueryLimit.CoreFilterTags.Select(tag => new QueryTag(tag)).ToList();

    public void Dispose()
    {
        _queryTagCache.Dispose();
        GC.SuppressFinalize(this);
    }

    public Task<IReadOnlyCollection<QueryTag>> GetQueryTagsAsync(CancellationToken cancellationToken = default)
        => _queryTagCache.GetAsync(cancellationToken: cancellationToken);

    private async Task<IReadOnlyCollection<QueryTag>> ResolveQueryTagsAsync(CancellationToken cancellationToken)
    {
        var tags = new List<QueryTag>(CoreQueryTags);
        IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(int.MaxValue, 0, cancellationToken);
        tags.AddRange(extendedQueryTags.Select(entry => new QueryTag(entry)));

        return tags;
    }
}
