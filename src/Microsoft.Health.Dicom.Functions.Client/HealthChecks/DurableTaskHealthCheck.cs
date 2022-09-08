// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : IHealthCheck
{
    private readonly IDurableClient _client;
    private readonly ILogger _logger;

    public DurableTaskHealthCheck(IDurableClientFactory factory, ILogger<DurableTaskHealthCheck> logger)
    {
        _client = EnsureArg.IsNotNull(factory, nameof(factory)).CreateClient();
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        DateTime start = Clock.UtcNow.DateTime;
        await _client.ListInstancesAsync(
            new OrchestrationStatusQueryCondition
            {
                CreatedTimeFrom = start,
                CreatedTimeTo = start.AddMinutes(1),
                PageSize = 1,
            },
            cancellationToken);

        _logger.LogInformation("Successfully connected to the Durable TaskHub '{Name}.'", _client.TaskHubName);
        return HealthCheckResult.Healthy();
    }
}
