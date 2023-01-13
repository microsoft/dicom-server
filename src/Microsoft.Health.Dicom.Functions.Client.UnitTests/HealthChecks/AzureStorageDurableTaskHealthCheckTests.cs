// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.HealthChecks;

public class AzureStorageDurableTaskHealthCheckTests
{
    private const string TaskHubName = "TestTaskHub";
    private readonly TaskHubInfo _taskHubInfo = new TaskHubInfo { CreatedAt = DateTime.UtcNow, PartitionCount = 12, TaskHubName = TaskHubName };
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>("UseDevelopmentStorage=true");
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
    private readonly TableServiceClient _tableServiceClient = Substitute.For<TableServiceClient>("UseDevelopmentStorage=true");
    private readonly AzureStorageDurableTaskHealthCheck _healthCheck;

    public AzureStorageDurableTaskHealthCheckTests()
    {
        IConfigurationSection section = Substitute.For<IConfigurationSection>();
        TokenCredential tokenCredential = Substitute.For<TokenCredential>();
        AzureComponentFactory azureComponentFactory = Substitute.For<AzureComponentFactory>();
        IConnectionInfoResolver connectionInfoResolver = Substitute.For<IConnectionInfoResolver>();
        var blobClientOptions = new BlobClientOptions();
        var queueClientOptions = new QueueClientOptions();
        var tableClientOptions = new TableClientOptions();

        section.Value.Returns((string)null);
        connectionInfoResolver.Resolve("AzureWebJobsStorage").Returns(section);
        azureComponentFactory.CreateTokenCredential(section).Returns(tokenCredential);

        azureComponentFactory.CreateClientOptions(typeof(BlobClientOptions), null, section).Returns(blobClientOptions);
        azureComponentFactory.CreateClientOptions(typeof(QueueClientOptions), null, section).Returns(queueClientOptions);
        azureComponentFactory.CreateClientOptions(typeof(TableClientOptions), null, section).Returns(tableClientOptions);

        azureComponentFactory.CreateClient(typeof(BlobServiceClient), section, tokenCredential, blobClientOptions).Returns(_blobServiceClient);
        azureComponentFactory.CreateClient(typeof(QueueServiceClient), section, tokenCredential, queueClientOptions).Returns(_queueServiceClient);
        azureComponentFactory.CreateClient(typeof(TableServiceClient), section, tokenCredential, tableClientOptions).Returns(_tableServiceClient);

        _healthCheck = new AzureStorageDurableTaskHealthCheck(
            azureComponentFactory,
            connectionInfoResolver,
            Options.Create(new DurableClientOptions { ConnectionName = "AzureWebJobsStorage", TaskHub = TaskHubName }),
            NullLogger<AzureStorageDurableTaskHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenMissingLeases_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        await using AsyncAssertionScope leases = ConfigureLeasesContainer(tokenSource.Token, healthy: false);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task GivenMissingControlQueues_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        await using AsyncAssertionScope leases = ConfigureLeasesContainer(tokenSource.Token);
        await using AsyncAssertionScope controlQueue = ConfigureControlQueues(tokenSource.Token, healthy: false);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task GivenMissingWorkItemQueue_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        await using AsyncAssertionScope leases = ConfigureLeasesContainer(tokenSource.Token);
        await using AsyncAssertionScope controlQueue = ConfigureControlQueues(tokenSource.Token);
        await using AsyncAssertionScope workItemQueue = ConfigureWorkItemQueue(tokenSource.Token, healthy: false);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task GivenMissingInstanceTable_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        await using AsyncAssertionScope leases = ConfigureLeasesContainer(tokenSource.Token);
        await using AsyncAssertionScope controlQueue = ConfigureControlQueues(tokenSource.Token);
        await using AsyncAssertionScope workItemQueue = ConfigureWorkItemQueue(tokenSource.Token);
        await using AsyncAssertionScope instanceTable = ConfigureInstanceTable(tokenSource.Token, healthy: false);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task GivenMissingHistoryTable_WhenCheckingHealth_ThenReturnUnhealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        await using AsyncAssertionScope leases = ConfigureLeasesContainer(tokenSource.Token);
        await using AsyncAssertionScope controlQueue = ConfigureControlQueues(tokenSource.Token);
        await using AsyncAssertionScope workItemQueue = ConfigureWorkItemQueue(tokenSource.Token);
        await using AsyncAssertionScope instanceTable = ConfigureInstanceTable(tokenSource.Token);
        await using AsyncAssertionScope historyTable = ConfigureHistoryTable(tokenSource.Token, healthy: false);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task GivenAvailableTaskHub_WhenCheckingHealth_ThenReturnHealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        await using AsyncAssertionScope leases = ConfigureLeasesContainer(tokenSource.Token);
        await using AsyncAssertionScope controlQueue = ConfigureControlQueues(tokenSource.Token);
        await using AsyncAssertionScope workItemQueue = ConfigureWorkItemQueue(tokenSource.Token);
        await using AsyncAssertionScope instanceTable = ConfigureInstanceTable(tokenSource.Token);
        await using AsyncAssertionScope historyTable = ConfigureHistoryTable(tokenSource.Token);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private AsyncAssertionScope ConfigureLeasesContainer(CancellationToken token, bool healthy = true)
    {
        string containerName = LeasesContainer.GetName(TaskHubName);
        BlobContainerClient blobContainerClient = Substitute.For<BlobContainerClient>("UseDevelopmentStorage=true", containerName);
        BlobClient blobClient = Substitute.For<BlobClient>("UseDevelopmentStorage=true", containerName, LeasesContainer.TaskHubBlobName);

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(blobContainerClient);
        blobContainerClient.GetBlobClient(LeasesContainer.TaskHubBlobName).Returns(blobClient);

        if (healthy)
        {
            BlobDownloadResult result = BlobDownloadResultFactory.CreateResult(new BinaryData(JsonSerializer.Serialize(_taskHubInfo)));
            blobClient.DownloadContentAsync(token).Returns(Response.FromValue(result, Substitute.For<Response>()));
        }
        else
        {
            blobClient
            .DownloadContentAsync(token)
            .Returns(
                Task.FromException<Response<BlobDownloadResult>>(
                    new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found.")));
        }

        return new AsyncAssertionScope(async () =>
        {
            _blobServiceClient.Received(1).GetBlobContainerClient(containerName);
            blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
            await blobClient.Received(1).DownloadContentAsync(token);
        });
    }

    private AsyncAssertionScope ConfigureControlQueues(CancellationToken token, bool healthy = true)
    {
        var clients = new QueueClient[_taskHubInfo.PartitionCount];
        for (int i = 0; i < clients.Length; i++)
        {
            string name = ControlQueues.GetName(TaskHubName, i);
            bool exists = healthy || (i < _taskHubInfo.PartitionCount - 1);
            clients[i] = Substitute.For<QueueClient>("UseDevelopmentStorage=true", name);
            clients[i].Name.Returns(name);
            clients[i].ExistsAsync(token).Returns(Task.FromResult(Response.FromValue(exists, Substitute.For<Response>())));

            _queueServiceClient.GetQueueClient(name).Returns(clients[i]);
        }

        return new AsyncAssertionScope(async () =>
        {
            for (int i = 0; i < clients.Length; i++)
            {
                _queueServiceClient.Received(1).GetQueueClient(ControlQueues.GetName(TaskHubName, i));
                await clients[i].Received(1).ExistsAsync(token);
            }
        });
    }

    private AsyncAssertionScope ConfigureWorkItemQueue(CancellationToken token, bool healthy = true)
    {
        string queueName = WorkItemQueue.GetName(TaskHubName);
        QueueClient queueClient = Substitute.For<QueueClient>("UseDevelopmentStorage=true", queueName);
        _queueServiceClient.GetQueueClient(queueName).Returns(queueClient);
        queueClient.ExistsAsync(token).Returns(Response.FromValue(healthy, Substitute.For<Response>()));

        return new AsyncAssertionScope(async () =>
        {
            _queueServiceClient.Received(1).GetQueueClient(queueName);
            await queueClient.Received(1).ExistsAsync(token);
        });
    }

    private AsyncAssertionScope ConfigureInstanceTable(CancellationToken token, bool healthy = true)
        => ConfigureTable(InstanceTable.GetName(TaskHubName), token, healthy);

    private AsyncAssertionScope ConfigureHistoryTable(CancellationToken token, bool healthy = true)
        => ConfigureTable(HistoryTable.GetName(TaskHubName), token, healthy);

    private AsyncAssertionScope ConfigureTable(string tableName, CancellationToken token, bool healthy = true)
    {
        TableClient tableClient = Substitute.For<TableClient>("UseDevelopmentStorage=true", tableName);
        IAsyncEnumerator<TableEntity> asyncEnumerator = Substitute.For<IAsyncEnumerator<TableEntity>>();
        AsyncPageable<TableEntity> asyncPageable = Substitute.For<AsyncPageable<TableEntity>>();
        _tableServiceClient.GetTableClient(tableName).Returns(tableClient);
        tableClient.QueryAsync<TableEntity>("Partition eq null", 1, null, token).Returns(asyncPageable);
        asyncPageable.GetAsyncEnumerator(token).Returns(asyncEnumerator);

        if (healthy)
        {
            asyncEnumerator.MoveNextAsync(token).Returns(info => ValueTask.FromResult(true));
        }
        else
        {
            asyncEnumerator
                .MoveNextAsync(token)
                .Returns(info => ValueTask.FromException<bool>(new RequestFailedException((int)HttpStatusCode.NotFound, "Cannot find table")));
        }

        return new AsyncAssertionScope(async () =>
        {
            _tableServiceClient.Received(1).GetTableClient(tableName);
            tableClient.Received(1).QueryAsync<TableEntity>("Partition eq null", 1, null, token);
            asyncPageable.Received(1).GetAsyncEnumerator(token);
            await asyncEnumerator.Received(1).MoveNextAsync(token);
        });
    }

    private sealed class AsyncAssertionScope : IAsyncDisposable
    {
        private readonly Func<ValueTask> _assertion;

        public AsyncAssertionScope(Func<ValueTask> assertion)
            => _assertion = assertion;

        public ValueTask DisposeAsync()
            => _assertion();
    }
}
