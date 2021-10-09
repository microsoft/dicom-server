// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal sealed class SqlIndexDataStore : IIndexDataStore
    {
        private readonly VersionedCache<ISqlIndexDataStore> _cache;

        public SqlIndexDataStore(VersionedCache<ISqlIndexDataStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<long> BeginCreateInstanceIndexAsync(DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.BeginCreateInstanceIndexAsync(dicomDataset, queryTags, partitionName, cancellationToken);
        }

        public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);
        }

        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, partitionName, cancellationToken);
        }

        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cleanupAfter, partitionName, cancellationToken);
        }

        public async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteStudyIndexAsync(studyInstanceUid, cleanupAfter, partitionName, cancellationToken);
        }

        public async Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetOldestDeletedAsync(cancellationToken);
        }

        public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier, cleanupAfter, cancellationToken);
        }

        public async Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, string partitionName = DefaultPartition.Name, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.ReindexInstanceAsync(dicomDataset, watermark, queryTags, partitionName, cancellationToken);
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);
        }

        public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(maxNumberOfRetries, cancellationToken);
        }

        public async Task EndCreateInstanceIndexAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, string partitionName = DefaultPartition.Name, bool allowExpiredTags = false, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.EndCreateInstanceIndexAsync(dicomDataset, watermark, queryTags, partitionName, allowExpiredTags, cancellationToken);
        }
    }
}
