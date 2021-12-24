// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Workitem
{
    internal sealed class SqlWorkitemStore : IWorkitemStore
    {
        private readonly VersionedCache<ISqlWorkitemStore> _cache;

        public SqlWorkitemStore(VersionedCache<ISqlWorkitemStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<long> AddWorkitemAsync(int partitionKey, WorkitemDataset dataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            ISqlWorkitemStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.AddWorkitemAsync(partitionKey, dataset, queryTags, cancellationToken);
        }
    }
}
