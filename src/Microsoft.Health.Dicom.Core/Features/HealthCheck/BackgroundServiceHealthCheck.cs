// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;

namespace Microsoft.Health.Dicom.Core.Features.HealthCheck;

public class BackgroundServiceHealthCheck : IHealthCheck
{
    private readonly IIndexDataStore _indexDataStore;
    private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
    private readonly DeleteMeter _deleteMeter;
    private readonly BackgroundServiceHealthCheckCache _backgroundServiceHealthCheckCache;
    private readonly ILogger<BackgroundServiceHealthCheck> _logger;

    public BackgroundServiceHealthCheck(
        IIndexDataStore indexDataStore,
        IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
        DeleteMeter deleteMeter,
        BackgroundServiceHealthCheckCache backgroundServiceHealthCheckCache,
        ILogger<BackgroundServiceHealthCheck> logger)
    {
        EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
        EnsureArg.IsNotNull(deleteMeter, nameof(deleteMeter));
        EnsureArg.IsNotNull(backgroundServiceHealthCheckCache, nameof(backgroundServiceHealthCheckCache));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _indexDataStore = indexDataStore;
        _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
        _deleteMeter = deleteMeter;
        _backgroundServiceHealthCheckCache = backgroundServiceHealthCheckCache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Task<DateTimeOffset> oldestWaitingToBeDeleted = _backgroundServiceHealthCheckCache.GetOrAddOldestTimeAsync(_indexDataStore.GetOldestDeletedAsync, cancellationToken);
            Task<int> numReachedMaxedRetry = _backgroundServiceHealthCheckCache.GetOrAddNumExhaustedDeletionAttemptsAsync(
                t => _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(_deletedInstanceCleanupConfiguration.MaxRetries, t),
                cancellationToken);

            _deleteMeter.OldestRequestedDeletion.Add((await oldestWaitingToBeDeleted).ToUnixTimeSeconds());
            _deleteMeter.CountDeletionsMaxRetry.Add(await numReachedMaxedRetry);
        }
        catch (DataStoreNotReadyException)
        {
            return HealthCheckResult.Unhealthy("Unhealthy service.");
        }

        return HealthCheckResult.Healthy("Successfully computed values for background service.");
    }
}
