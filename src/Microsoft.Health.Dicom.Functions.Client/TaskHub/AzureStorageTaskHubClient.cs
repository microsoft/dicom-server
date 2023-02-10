// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Client.Extensions;

namespace Microsoft.Health.Dicom.Functions.Client.TaskHub;

internal class AzureStorageTaskHubClient : ITaskHubClient
{
    private readonly LeasesContainer _leasesContainer;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AzureStorageTaskHubClient> _logger;

    public AzureStorageTaskHubClient(
        AzureComponentFactory factory,
        IConnectionInfoResolver connectionInfoProvider,
        IOptions<DurableClientOptions> options,
        ILoggerFactory loggerFactory)
    {
        EnsureArg.IsNotNull(factory, nameof(factory));
        DurableClientOptions clientOptions = EnsureArg.IsNotNull(options?.Value, nameof(options));
        IConfigurationSection connectionSection = EnsureArg.IsNotNull(connectionInfoProvider, nameof(connectionInfoProvider)).Resolve(clientOptions.ConnectionName);

        TaskHubName = EnsureArg.IsNotNullOrWhiteSpace(clientOptions.TaskHub, nameof(clientOptions));
        _leasesContainer = new LeasesContainer(factory.CreateBlobServiceClient(connectionSection), clientOptions.TaskHub);
        _queueServiceClient = factory.CreateQueueServiceClient(connectionSection);
        _tableServiceClient = factory.CreateTableServiceClient(connectionSection);
        _loggerFactory = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<AzureStorageTaskHubClient>();
    }

    internal AzureStorageTaskHubClient(
        string name,
        LeasesContainer leasesContainer,
        QueueServiceClient queueServiceClient,
        TableServiceClient tableServiceClient,
        ILoggerFactory loggerFactory)
    {
        TaskHubName = EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));
        _leasesContainer = EnsureArg.IsNotNull(leasesContainer, nameof(leasesContainer));
        _queueServiceClient = EnsureArg.IsNotNull(queueServiceClient, nameof(queueServiceClient));
        _tableServiceClient = EnsureArg.IsNotNull(tableServiceClient, nameof(tableServiceClient));
        _loggerFactory = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<AzureStorageTaskHubClient>();
    }

    public string TaskHubName { get; }

    public async ValueTask<ITaskHub> GetTaskHubAsync(CancellationToken cancellationToken = default)
    {
        TaskHubInfo taskHubInfo = await _leasesContainer.GetTaskHubInfoAsync(cancellationToken);
        if (taskHubInfo == null)
        {
            _logger.LogWarning("Cannot find leases blob container '{LeasesContainer}.'", _leasesContainer.Name);
            return null;
        }

        return new AzureStorageTaskHub(
            new ControlQueueCollection(_queueServiceClient, taskHubInfo),
            new WorkItemQueue(_queueServiceClient, taskHubInfo.TaskHubName),
            new InstanceTable(_tableServiceClient, taskHubInfo.TaskHubName),
            new HistoryTable(_tableServiceClient, taskHubInfo.TaskHubName),
            _loggerFactory.CreateLogger<AzureStorageTaskHub>());
    }
}
