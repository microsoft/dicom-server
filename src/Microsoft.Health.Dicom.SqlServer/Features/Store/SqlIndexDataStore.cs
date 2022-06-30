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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
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

    public async Task<long> BeginCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.BeginCreateInstanceIndexAsync(partitionKey, dicomDataset, queryTags, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task DeleteDeletedInstanceAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteDeletedInstanceAsync(versionedInstanceIdentifier, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task DeleteInstanceIndexAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteInstanceIndexAsync(partitionKey, studyInstanceUid, seriesInstanceUid, sopInstanceUid, cleanupAfter, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task DeleteSeriesIndexAsync(int partitionKey, string studyInstanceUid, string seriesInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteSeriesIndexAsync(partitionKey, studyInstanceUid, seriesInstanceUid, cleanupAfter, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task DeleteStudyIndexAsync(int partitionKey, string studyInstanceUid, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.DeleteStudyIndexAsync(partitionKey, studyInstanceUid, cleanupAfter, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task<DateTimeOffset> GetOldestDeletedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.GetOldestDeletedAsync(cancellationToken);
        }
        catch (DataStoreException ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task<int> IncrementDeletedInstanceRetryAsync(VersionedInstanceIdentifier versionedInstanceIdentifier, DateTimeOffset cleanupAfter, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.IncrementDeletedInstanceRetryAsync(versionedInstanceIdentifier, cleanupAfter, cancellationToken);
        }
        catch (DataStoreException ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task ReindexInstanceAsync(DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.ReindexInstanceAsync(dicomDataset, watermark, queryTags, cancellationToken);
        }
        catch (DataStoreException ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task<IEnumerable<VersionedInstanceIdentifier>> RetrieveDeletedInstancesAsync(int batchSize, int maxRetries, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.RetrieveDeletedInstancesAsync(batchSize, maxRetries, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task<int> RetrieveNumExhaustedDeletedInstanceAttemptsAsync(int maxNumberOfRetries, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            return await store.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(maxNumberOfRetries, cancellationToken);
        }
        catch (DataStoreException ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }

    public async Task EndCreateInstanceIndexAsync(int partitionKey, DicomDataset dicomDataset, long watermark, IEnumerable<QueryTag> queryTags, bool allowExpiredTags = false, bool hasFrameMetadata = false, CancellationToken cancellationToken = default)
    {
        try
        {
            ISqlIndexDataStore store = await _cache.GetAsync(cancellationToken: cancellationToken);
            await store.EndCreateInstanceIndexAsync(partitionKey, dicomDataset, watermark, queryTags, allowExpiredTags, hasFrameMetadata, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The operation failed.");

            throw;
        }
    }
}
