// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using Microsoft.Health.Encryption.Customer.Health;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : AzureStorageHealthCheck
{
    private readonly ITaskHubClient _client;
    private readonly ILogger<DurableTaskHealthCheck> _logger;

    public DurableTaskHealthCheck(
        ITaskHubClient client,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<DurableTaskHealthCheck> logger)
        : base(customerKeyHealthCache, logger)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public override async Task<HealthCheckResult> CheckAzureStorageHealthAsync(CancellationToken cancellationToken)
    {
        ITaskHub taskHub = await _client.GetTaskHubAsync(cancellationToken).ConfigureAwait(false);
        if (taskHub == null)
            return HealthCheckResult.Unhealthy("Task hub not found.");

        if (!await taskHub.IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            return HealthCheckResult.Unhealthy("Task hub is not ready.");

        _logger.LogInformation("Successfully connected to the task hub");
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
