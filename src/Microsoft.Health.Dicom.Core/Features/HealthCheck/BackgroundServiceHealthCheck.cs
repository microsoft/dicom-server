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

        public BackgroundServiceHealthCheck(
            IIndexDataStoreFactory indexDataStoreFactory,
            IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
            TelemetryClient telemetryClient)
        {
            EnsureArg.IsNotNull(indexDataStoreFactory, nameof(indexDataStoreFactory));
            EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
            EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));


            _indexDataStore = indexDataStoreFactory.GetInstance();
            _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
            _telemetryClient = telemetryClient;
        }


        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            DateTimeOffset oldestWaitingToBeDeleated = await _indexDataStore.GetOldestDeletedAsync(cancellationToken);
            int numReachedMaxedRetry = await _indexDataStore.RetrieveNumDeletedExceedRetryCountAsync(_deletedInstanceCleanupConfiguration.MaxRetries, cancellationToken);
           
            _telemetryClient.GetMetric("Oldest-Requested-Deletion").TrackValue(oldestWaitingToBeDeleated.ToUnixTimeSeconds());
            _telemetryClient.GetMetric("Count-Deletions-Max-Retry").TrackValue(numReachedMaxedRetry);

            return HealthCheckResult.Healthy("Successfully computed values for background service.");
        }
    }
}
