// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class DurableTaskHealthCheck : IHealthCheck
{
    private readonly IDurableClient _client;
    private readonly IGuidFactory _guidFactory;
    private readonly ILogger _logger;

    public DurableTaskHealthCheck(IDurableClientFactory factory, IGuidFactory guidFactory, ILogger<DurableTaskHealthCheck> logger)
    {
        _client = EnsureArg.IsNotNull(factory, nameof(factory)).CreateClient();
        _guidFactory = EnsureArg.IsNotNull(guidFactory, nameof(guidFactory));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        await AssertTaskHubConnectionAsync(cancellationToken);

        _logger.LogInformation("Successfully connected to the Durable TaskHub '{Name}.'", _client.TaskHubName);
        return HealthCheckResult.Healthy();
    }

    private Task AssertTaskHubConnectionAsync(CancellationToken cancellationToken)
    {
        // Attempt to query the state the orchestrations. The results of the query do not matter.
        // We simply want to run a relatively "cheap" query against the table storage to ensure we can connect successfully.
        cancellationToken.ThrowIfCancellationRequested();

        return _client.GetStatusAsync(
            _guidFactory.Create().ToString(OperationId.FormatSpecifier),
            showHistory: false,
            showHistoryOutput: false,
            showInput: false);
    }
}
