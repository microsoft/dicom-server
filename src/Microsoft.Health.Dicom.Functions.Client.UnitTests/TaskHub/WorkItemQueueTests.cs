// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class WorkItemQueueTests
{
    [Fact]
    public async Task GivenMissingQueue_WhenCheckingExistence_ThenReturnFalse()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        string queueName = WorkItemQueue.GetName(TaskHubName);
        using var tokenSource = new CancellationTokenSource();

        QueueServiceClient queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
        QueueClient queueClient = Substitute.For<QueueClient>("UseDevelopmentStorage=true", queueName);
        queueServiceClient.GetQueueClient(queueName).Returns(queueClient);
        queueClient.ExistsAsync(tokenSource.Token).Returns(Response.FromValue(false, Substitute.For<Response>()));

        // Test
        var workItemQueue = new WorkItemQueue(queueServiceClient, TaskHubName);
        Assert.False(await workItemQueue.ExistsAsync(tokenSource.Token));

        queueServiceClient.Received(1).GetQueueClient(queueName);
        await queueClient.Received(1).ExistsAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableQueues_WhenCheckingExistence_ThenReturnTrue()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        string queueName = WorkItemQueue.GetName(TaskHubName);
        using var tokenSource = new CancellationTokenSource();

        QueueServiceClient queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
        QueueClient queueClient = Substitute.For<QueueClient>("UseDevelopmentStorage=true", queueName);
        queueServiceClient.GetQueueClient(queueName).Returns(queueClient);
        queueClient.ExistsAsync(tokenSource.Token).Returns(Response.FromValue(true, Substitute.For<Response>()));

        // Test
        var workItemQueue = new WorkItemQueue(queueServiceClient, TaskHubName);
        Assert.True(await workItemQueue.ExistsAsync(tokenSource.Token));

        queueServiceClient.Received(1).GetQueueClient(queueName);
        await queueClient.Received(1).ExistsAsync(tokenSource.Token);
    }
}
