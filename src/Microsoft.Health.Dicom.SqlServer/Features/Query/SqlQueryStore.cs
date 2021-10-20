// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    internal class SqlQueryStore : IQueryStore
    {
        private readonly VersionedCache<ISqlQueryStore> _cache;

        public SqlQueryStore(VersionedCache<ISqlQueryStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<QueryResult> QueryAsync(
            QueryExpression query,
            CancellationToken cancellationToken)
        {
            ISqlQueryStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.QueryAsync(query, cancellationToken);
        }
    }
}
