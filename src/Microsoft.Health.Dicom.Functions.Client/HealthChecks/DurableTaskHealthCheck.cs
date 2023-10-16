// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using Microsoft.Health.Encryption.Customer.Health;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : IHealthCheck
{
    private const string DegradedDescription = "The health of the task hub has degraded.";

    private readonly ITaskHubClient _client;
    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache;
    private readonly ILogger<DurableTaskHealthCheck> _logger;

    public DurableTaskHealthCheck(
        ITaskHubClient client,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<DurableTaskHealthCheck> logger)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _customerKeyHealthCache = EnsureArg.IsNotNull(customerKeyHealthCache, nameof(customerKeyHealthCache));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        CustomerKeyHealth cmkStatus = await _customerKeyHealthCache.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!cmkStatus.IsHealthy)
        {
            // if the customer-managed key is inaccessible, the task hub will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                cmkStatus.Exception,
                new Dictionary<string, object> { { "Reason", cmkStatus.Reason } });
        }

        ITaskHub taskHub = await _client.GetTaskHubAsync(cancellationToken).ConfigureAwait(false);
        if (taskHub == null)
            return HealthCheckResult.Unhealthy("Task hub not found.");

        if (!await taskHub.IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            return HealthCheckResult.Unhealthy("Task hub is not ready.");

        _logger.LogInformation("Successfully connected to the task hub");
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
