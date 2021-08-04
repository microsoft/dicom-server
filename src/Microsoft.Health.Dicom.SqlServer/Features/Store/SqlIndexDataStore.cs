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
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store
{
    internal sealed class SqlIndexDataStore : IIndexDataStore
    {
        private readonly VersionedCache<ISqlIndexDataStore> _cache;

        public SqlIndexDataStore(VersionedCache<ISqlIndexDataStore> cache)
            => _cache = EnsureArg.IsNotNull(cache, nameof(cache));

        public async Task<long> CreateInstanceIndexAsync(DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, ExtendedQueryTagsVersion extendedQueryTagETag, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            return await store.CreateInstanceIndexAsync(dicomDataset, queryTags, extendedQueryTagETag, cancellationToken);
        }

        public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            await store.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);
        }

        public async Task DeleteInstanceIndexAsync(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            await store.DeleteInstanceIndexAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteSeriesIndexAsync(string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            await store.DeleteSeriesIndexAsync(studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task DeleteStudyIndexAsync(string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            await store.DeleteStudyIndexAsync(studyInstanceUid, cleanupAfter, cancellationToken);
        }

        public async Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            return await store.GetOldestDeletedAsync(cancellationToken);
        }

        public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            return await store.IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier, cleanupAfter, cancellationToken);
        }

        public async Task ReindexInstanceAsync(DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            await store.ReindexInstanceAsync(dicomDataset, queryTags, cancellationToken);
        }

        public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            return await store.RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);
        }

        public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            return await store.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(maxNumberOfRetries, cancellationToken);
        }

        public async Task UpdateInstanceIndexStatusAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, IndexStatus status, CancellationToken cancellationToken = default)
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken);
            await store.UpdateInstanceIndexStatusAsync(versionedInstanceIdentifier, status, cancellationToken);
        }
    }
}
