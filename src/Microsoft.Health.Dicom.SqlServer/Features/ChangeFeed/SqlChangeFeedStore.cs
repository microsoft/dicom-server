// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed
{
    internal class SqlChangeFeedStore : IChangeFeedStore
    {
        private readonly VersionedCache<ISqlChangeFeedStore> _cache;

        public SqlChangeFeedStore(VersionedCache<ISqlChangeFeedStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<ChangeFeedEntry> GetChangeFeedLatestAsync(CancellationToken cancellationToken)
        {
            ISqlChangeFeedStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetChangeFeedLatestAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<ChangeFeedEntry>> GetChangeFeedAsync(long offset, int limit, CancellationToken cancellationToken)
        {
            ISqlChangeFeedStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetChangeFeedAsync(offset, limit, cancellationToken);
        }
    }
}
