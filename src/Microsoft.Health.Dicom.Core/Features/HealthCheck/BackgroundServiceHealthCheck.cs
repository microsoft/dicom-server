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
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Store;

namespace Microsoft.Health.Dicom.Core.Features.HealthCheck
{
    public class BackgroundServiceHealthCheck : IHealthCheck
    {
        private readonly IIndexDataStore _indexDataStore;
        private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
        private readonly TelemetryClient _telemetryClient;
        private readonly BackgroundServiceHealthCheckCache _backgroundServiceHealthCheckCache;

        public BackgroundServiceHealthCheck(
            IIndexDataStoreFactory indexDataStoreFactory,
            IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
            TelemetryClient telemetryClient,
            BackgroundServiceHealthCheckCache backgroundServiceHealthCheckCache)
        {
            EnsureArg.IsNotNull(indexDataStoreFactory, nameof(indexDataStoreFactory));
            EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
            EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));
            EnsureArg.IsNotNull(backgroundServiceHealthCheckCache, nameof(backgroundServiceHealthCheckCache));

            _indexDataStore = indexDataStoreFactory.GetInstance();
            _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
            _telemetryClient = telemetryClient;
            _backgroundServiceHealthCheckCache = backgroundServiceHealthCheckCache;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Task<DateTimeOffset> oldestWaitingToBeDeleated = _backgroundServiceHealthCheckCache.GetOrAddOldestTimeAsync(_indexDataStore.GetOldestDeletedAsync, cancellationToken);
            Task<int> numReachedMaxedRetry = _backgroundServiceHealthCheckCache.GetOrAddNumExhaustedDeletionAttemptsAsync(
                t => _indexDataStore.RetrieveNumExhaustedDeletedInstanceAttemptsAsync(_deletedInstanceCleanupConfiguration.MaxRetries, t),
                cancellationToken);

            _telemetryClient.GetMetric("Oldest-Requested-Deletion").TrackValue((await oldestWaitingToBeDeleated).ToUnixTimeSeconds());
            _telemetryClient.GetMetric("Count-Deletions-Max-Retry").TrackValue(await numReachedMaxedRetry);

            return HealthCheckResult.Healthy("Successfully computed values for background service.");
        }
    }
}
