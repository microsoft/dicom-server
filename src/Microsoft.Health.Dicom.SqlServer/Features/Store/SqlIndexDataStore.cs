// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.SqlServer.Features.Store;

internal sealed class SqlIndexDataStore : IIndexDataStore
{
    private readonly VersionedCache<ISqlIndexDataStore> _cache;
    private readonly ILogger<SqlIndexDataStore> _logger;

    public SqlIndexDataStore(VersionedCache<ISqlIndexDataStore> cache, ILogger<SqlIndexDataStore> logger)
    {
        _cache = EnsureArg.IsNotNull(cache, nameof(cache));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<long> BeginCreateInstanceIndexAsync(Partition partition, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.BeginCreateInstanceIndexAsync(partition, dicomDataset, queryTags, cancellationToken);
    }

    public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        await store.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);
    }

    public async Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteInstanceIndexAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.DeleteInstanceIndexAsync(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
    }

    public async Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteSeriesIndexAsync(Partition partition, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.DeleteSeriesIndexAsync(partition, studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
    }

    public async Task<IReadOnlyCollection<VersionedInstanceIdentifier>> DeleteStudyIndexAsync(Partition partition, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.DeleteStudyIndexAsync(partition, studyInstanceUid, cleanupAfter, cancellationToken);
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

    public async Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        await store.ReindexInstanceAsync(dicomDataset, watermark, queryTags, cancellationToken);
    }

    public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);
    }

    public async Task<IReadOnlyList<InstanceMetadata>> RetrieveDeletedInstancesWithPropertiesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.RetrieveDeletedInstancesWithPropertiesAsync(batchSize, maxRetries, cancellationToken);
    }

    public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(maxNumberOfRetries, cancellationToken);
    }

    public async Task EndCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, FileProperties fileProperties, bool allowExpiredTags = false, bool hasFrameMetadata = false, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        await store.EndCreateInstanceIndexAsync(partitionKey, dicomDataset, watermark, queryTags, fileProperties, allowExpiredTags, hasFrameMetadata, cancellationToken);
    }

    public async Task<IEnumerable<InstanceMetadata>> BeginUpdateInstanceAsync(Partition partition, IReadOnlyCollection<long> versions, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.BeginUpdateInstanceAsync(partition, versions, cancellationToken);
    }

    public async Task<IReadOnlyList<InstanceMetadata>> BeginUpdateInstancesAsync(Partition partition, string studyInstanceUid, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.BeginUpdateInstancesAsync(partition, studyInstanceUid, cancellationToken);
    }

    public async Task EndUpdateInstanceAsync(int partitionKey, string studyInstanceUid, DicomDataset dicomDataset, IReadOnlyList<InstanceMetadata> instanceMetadataList, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        await store.EndUpdateInstanceAsync(partitionKey, studyInstanceUid, dicomDataset, instanceMetadataList, queryTags, cancellationToken);
    }

    public async Task UpdateFrameDataAsync(int partitionKey, IEnumerable<long> versions, bool hasFrameMetadata, CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        await store.UpdateFrameDataAsync(partitionKey, versions, hasFrameMetadata, cancellationToken);
    }

    public async Task UpdateFilePropertiesContentLengthAsync(
        IReadOnlyDictionary<long, FileProperties> filePropertiesByWatermark,
        CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        await store.UpdateFilePropertiesContentLengthAsync(filePropertiesByWatermark, cancellationToken);
    }

    public async Task<IndexedFileProperties> GetIndexedFileMetricsAsync(CancellationToken cancellationToken = default)
    {
        ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
        return await store.GetIndexedFileMetricsAsync(cancellationToken);
    }
}
