// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal sealed class DurableTaskHubMonitor : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DurableTaskHubMonitor> _logger;
    private readonly DurableTaskHubMonitorOptions _options;

    public DurableTaskHubMonitor(IServiceProvider serviceProvider, IOptions<DurableTaskHubMonitorOptions> options, ILogger<DurableTaskHubMonitor> logger)
    {
        _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (_options.Enabled)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            ITaskHubClient client = scope.ServiceProvider.GetRequiredService<ITaskHubClient>();

            try
            {
                ITaskHub taskHub = await client.GetTaskHubAsync(cancellationToken);

                if (taskHub == null)
                {
                    _logger.LogWarning("Task hub '{TaskHub}' does not exist. Will check again in {Interval}...", client.TaskHubName, _options.PollingInterval);
                }
                else if (!await taskHub.IsReadyAsync(cancellationToken))
                {
                    _logger.LogWarning("Task hub '{TaskHub}' is not ready yet. Will check again in {Interval}...", client.TaskHubName, _options.PollingInterval);
                }
                else
                {
                    _logger.LogInformation("Task hub '{TaskHub}' is ready.", client.TaskHubName);
                    break;
                }
            }
            catch (RequestFailedException e)
            {
                _logger.LogError(e, "Encountered an unexpected error when checking the task hub");
            }

            await Task.Delay(_options.PollingInterval, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
