// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;

namespace Microsoft.Health.Dicom.Functions.Client.HealthChecks;

internal sealed class AzureStorageDurableTaskHealthCheck : IHealthCheck
{
    private readonly string _taskHubName;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<AzureStorageDurableTaskHealthCheck> _logger;

    public AzureStorageDurableTaskHealthCheck(AzureComponentFactory factory, IConnectionInfoResolver connectionInfoProvider, IOptions<DurableClientOptions> options, ILogger<AzureStorageDurableTaskHealthCheck> logger)
    {
        EnsureArg.IsNotNull(factory, nameof(factory));

        DurableClientOptions clientOptions = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _taskHubName = clientOptions.TaskHub;

        IConfigurationSection connectionSection = EnsureArg.IsNotNull(connectionInfoProvider, nameof(connectionInfoProvider)).Resolve(clientOptions.ConnectionName);
        TokenCredential credential = connectionSection.Value is null ? factory.CreateTokenCredential(connectionSection) : null;
        _blobServiceClient = factory.CreateClient(typeof(BlobServiceClient), connectionSection, credential, null) as BlobServiceClient;
        _queueServiceClient = factory.CreateClient(typeof(QueueServiceClient), connectionSection, credential, null) as QueueServiceClient;
        _tableServiceClient = factory.CreateClient(typeof(TableServiceClient), connectionSection, credential, null) as TableServiceClient;

        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        var leasesContainer = new LeasesContainer(_blobServiceClient, _taskHubName);
        TaskHubInfo taskHubInfo = await leasesContainer.GetTaskHubInfoAsync(cancellationToken);
        if (taskHubInfo == null)
        {
            _logger.LogWarning("Cannot find leases blob container '{LeasesContainer}' for task hub '{TaskHub}.'", leasesContainer.Name, _taskHubName);
            return HealthCheckResult.Unhealthy("Task Hub not found.");
        }

        // Check that each of the components found in the Task Hub are available
        var controlQueues = new ControlQueues(_queueServiceClient, taskHubInfo);
        if (!await controlQueues.ExistAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find one or more of the control queues: [{ControlQueues}].", string.Join(", ", controlQueues.Names));
            return HealthCheckResult.Unhealthy("Task Hub is not ready.");
        }

        var workItemQueue = new WorkItemQueue(_queueServiceClient, taskHubInfo.TaskHubName);
        if (!await workItemQueue.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find work item queue '{WorkItemQueue}.'", workItemQueue.Name);
            return HealthCheckResult.Unhealthy("Task Hub is not ready.");
        }

        var instanceTable = new InstanceTable(_tableServiceClient, taskHubInfo.TaskHubName);
        if (!await instanceTable.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find instance table '{InstanceTable}.'", instanceTable.Name);
            return HealthCheckResult.Unhealthy("Task Hub is not ready.");
        }

        var historyTable = new HistoryTable(_tableServiceClient, taskHubInfo.TaskHubName);
        if (!await historyTable.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Cannot find history table '{HistoryTable}.'", historyTable.Name);
            return HealthCheckResult.Unhealthy("Task Hub is not ready.");
        }

        _logger.LogInformation("Successfully connected to the Durable Task Hub '{Name}.'", _taskHubName);
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
