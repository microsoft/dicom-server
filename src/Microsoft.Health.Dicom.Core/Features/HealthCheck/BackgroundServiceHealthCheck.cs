// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Encryption.Customer.Health;

namespace Microsoft.Health.Dicom.Core.Features.HealthCheck;

public class BackgroundServiceHealthCheck : IHealthCheck
{
    private const string DegradedDescription = "The health of the background service has degraded.";

    private readonly IIndexDataStore _indexDataStore;
    private readonly DeletedInstanceCleanupConfiguration _deletedInstanceCleanupConfiguration;
    private readonly DeleteMeter _deleteMeter;
    private readonly BackgroundServiceHealthCheckCache _backgroundServiceHealthCheckCache;
    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache;
    private readonly ILogger<BackgroundServiceHealthCheck> _logger;

    public BackgroundServiceHealthCheck(
        IIndexDataStore indexDataStore,
        IOptions<DeletedInstanceCleanupConfiguration> deletedInstanceCleanupConfiguration,
        DeleteMeter deleteMeter,
        BackgroundServiceHealthCheckCache backgroundServiceHealthCheckCache,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<BackgroundServiceHealthCheck> logger)
    {
        EnsureArg.IsNotNull(indexDataStore, nameof(indexDataStore));
        EnsureArg.IsNotNull(deletedInstanceCleanupConfiguration?.Value, nameof(deletedInstanceCleanupConfiguration));
        EnsureArg.IsNotNull(deleteMeter, nameof(deleteMeter));
        EnsureArg.IsNotNull(backgroundServiceHealthCheckCache, nameof(backgroundServiceHealthCheckCache));
        EnsureArg.IsNotNull(customerKeyHealthCache, nameof(customerKeyHealthCache));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _indexDataStore = indexDataStore;
        _deletedInstanceCleanupConfiguration = deletedInstanceCleanupConfiguration.Value;
        _deleteMeter = deleteMeter;
        _backgroundServiceHealthCheckCache = backgroundServiceHealthCheckCache;
        _customerKeyHealthCache = customerKeyHealthCache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            CustomerKeyHealth cmkStatus = await _customerKeyHealthCache.GetAsync(cancellationToken).ConfigureAwait(false);
            if (!cmkStatus.IsHealthy)
            {
                // if the customer-managed key is inaccessible, the data store will also be inaccessible
                return new HealthCheckResult(
                    HealthStatus.Degraded,
                    DegradedDescription,
                    cmkStatus.Exception,
                    new Dictionary<string, object> { { "Reason", cmkStatus.Reason } });
            }

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
