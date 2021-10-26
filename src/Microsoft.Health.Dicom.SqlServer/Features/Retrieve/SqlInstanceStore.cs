// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Retrieve
{
    internal sealed class SqlInstanceStore : IInstanceStore
    {
        private readonly VersionedCache<ISqlInstanceStore> _cache;

        public SqlInstanceStore(VersionedCache<ISqlInstanceStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifierAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetInstanceIdentifierAsync(partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cancellationToken);
        }

        public async Task<IReadOnlyList<VersionedInstanceIdentifier>> GetInstanceIdentifiersByWatermarkRangeAsync(WatermarkRange watermarkRange, IndexStatus indexStatus, CancellationToken cancellationToken = default)
        {
            ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetInstanceIdentifiersByWatermarkRangeAsync(watermarkRange, indexStatus, cancellationToken);
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInSeriesAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetInstanceIdentifiersInSeriesAsync(partitionKey, studyInstanceUid, seriesInstanceUid, cancellationToken);
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> GetInstanceIdentifiersInStudyAsync(int partitionKey, string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetInstanceIdentifiersInStudyAsync(partitionKey, studyInstanceUid, cancellationToken);
        }

        public async Task<IReadOnlyList<WatermarkRange>> GetInstanceBatchesAsync(int batchSize, int batchCount, IndexStatus indexStatus, long? maxWatermark = null, CancellationToken cancellationToken = default)
        {
            ISqlInstanceStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetInstanceBatchesAsync(batchSize, batchCount, indexStatus, maxWatermark, cancellationToken);
        }
    }
}
