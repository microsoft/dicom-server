// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve;

internal sealed class SqlInstanceStore : IInstanceStore
{
    private readonly VersionedCache<ISqlInstanceStore> _cache;

    public SqlInstanceStore(VersionedCache<ISqlInstanceStore> cache)
        => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

    public async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceIdentifierAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
    }

    public async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersByWatermarkRangeAsync(WatermarkRange watermarkRange, IndexStatus indexStatus, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, indexStatus, cancellationToken);
    }

    public async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceIdentifiersInSeriesAsync(partition, studyInstanceUid, seriesInstanceUid, cancellationToken);
    }

    public async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(Partition partition, string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceIdentifiersInStudyAsync(partition, studyInstanceUid, cancellationToken);
    }

    public async Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesAsync(int batchSize, int batchCount, IndexStatus indexStatus, long? maxWatermark = null, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceBatchesAsync(batchSize, batchCount, indexStatus, maxWatermark, cancellationToken);
    }

    public async Task<IReadOnlyList<InstanceMetadata>> GetInstanceIdentifierWithPropertiesAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid = null, string sopInstanceUid = null, bool isInitialVersion = false, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceIdentifierWithPropertiesAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid, isInitialVersion, cancellationToken);
    }
    public async Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesByTimeStampAsync(int batchSize, int batchCount, IndexStatus indexStatus, DateTimeOffset startTimeStamp, DateTimeOffset endTimeStamp, long? maxWatermark = null, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetInstanceBatchesByTimeStampAsync(batchSize, batchCount, indexStatus, startTimeStamp, endTimeStamp, maxWatermark, cancellationToken);
    }
    public async Task<IReadOnlyList<WatermarkRange>> GetContentLengthBackFillInstanceBatches(int batchSize, int batchCount, CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetContentLengthBackFillInstanceBatches(batchSize, batchCount, cancellationToken);
    }

    public async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(
        WatermarkRange watermarkRange,
        CancellationToken cancellationToken = default)
    {
        ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetContentLengthBackFillInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, cancellationToken);
    }
}
