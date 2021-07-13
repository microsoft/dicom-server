// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.HealthCheck
{
    public class BackgroundServiceHealthCheck : IHealthCheck
    {
        private readonly IStoreFactory<IIndexDataStore> _indexDataStoreFactory;
        private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
        private readonly TelemetryClient _telemetryClient;
        private readonly BackgroundServiceHealthCheckCache _backgroundServiceHealthCheckCache;
        private readonly ILogger<BackgroundServiceHealthCheck> _logger;

        public BackgroundServiceHealthCheck(
            IStoreFactory<IIndexDataStore> indexDataStoreFactory,
            IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
            TelemetryClient telemetryClient,
            BackgroundServiceHealthCheckCache backgroundServiceHealthCheckCache,
            ILogger<BackgroundServiceHealthCheck> logger)
        {
            EnsureArg.IsNotNull(indexDataStoreFactory, nameof(indexDataStoreFactory));
            EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
            EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
            EnsureArg.IsNotNull(backgroundServiceHealthCheckCache, nameof(backgroundServiceHealthCheckCache));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _indexDataStoreFactory = indexDataStoreFactory;
            _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
            _telemetryClient = telemetryClient;
            _backgroundServiceHealthCheckCache = backgroundServiceHealthCheckCache;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            IIndexDataStore indexDataStore = await _indexDataStoreFactory.GetInstanceAsync(cancellationToken);

            try
            {
                Task<DateTimeOffset> oldestWaitingToBeDeleated = _backgroundServiceHealthCheckCache.GetOrAddOldestTimeAsync(indexDataStore.GetOldestDeletedAsync, cancellationToken);
                Task<int> numReachedMaxedRetry = _backgroundServiceHealthCheckCache.GetOrAddNumExhaustedDeletionAttemptsAsync(
                    t => indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(_deletedInstanceCleanupConfiguration.MaxRetries, t),
                    cancellationToken);

                _telemetryClient.GetMetric("Oldest-Requested-Deletion").TrackValue((await oldestWaitingToBeDeleated).ToUnixTimeSeconds());
                _telemetryClient.GetMetric("Count-Deletions-Max-Retry").TrackValue(await numReachedMaxedRetry);
            }
            catch (DataStoreException e) // This is expected when service is starting up without schema initialization
            {
                _logger.LogError(e, "The service is unhealthy.");

                return HealthCheckResult.Unhealthy("Unhealthy service.");
            }

            return HealthCheckResult.Healthy("Successfully computed values for background service.");
        }
    }
}
