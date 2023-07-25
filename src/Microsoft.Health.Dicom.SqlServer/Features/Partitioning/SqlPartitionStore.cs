// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Partitioning;

internal sealed class SqlPartitionStore : IPartitionStore
{
    private readonly VersionedCache<ISqlPartitionStore> _cache;

    public SqlPartitionStore(VersionedCache<ISqlPartitionStore> cache)
        => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

    public async Task<Partition> AddPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        ISqlPartitionStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.AddPartitionAsync(partitionName, cancellationToken);
    }

    public async Task<IEnumerable<Partition>> GetPartitionsAsync(CancellationToken cancellationToken = default)
    {
        ISqlPartitionStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetPartitionsAsync(cancellationToken);
    }

    public async Task<Partition> GetPartitionAsync(string partitionName, CancellationToken cancellationToken = default)
    {
        ISqlPartitionStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetPartitionAsync(partitionName, cancellationToken);
    }
}
