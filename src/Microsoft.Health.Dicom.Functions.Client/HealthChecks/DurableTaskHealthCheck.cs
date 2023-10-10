// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : IHealthCheck
{
    private readonly ITaskHubClient _client;
    private readonly ILogger<DurableTaskHealthCheck> _logger;

    public DurableTaskHealthCheck(ITaskHubClient client, ILogger<DurableTaskHealthCheck> logger)
    {
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        ITaskHub taskHub = await _client.GetTaskHubAsync(cancellationToken);
        if (taskHub == null)
            return HealthCheckResult.Unhealthy("Task hub not found.");

        if (!await taskHub.IsHealthyAsync(cancellationToken))
            return HealthCheckResult.Unhealthy("Task hub is not ready.");

        _logger.LogInformation("Successfully connected to the task hub");
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
