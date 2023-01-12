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

public class ControlQueuesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GivenMissingQueue_WhenCheckingExistence_ThenReturnFalse(int j)
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        using var tokenSource = new CancellationTokenSource();

        QueueServiceClient queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
        var clients = new QueueClient[3];
        for (int i = 0; i < clients.Length; i++)
        {
            string name = ControlQueues.GetName(TaskHubName, i);
            clients[i] = Substitute.For<QueueClient>("UseDevelopmentStorage=true", name);
            clients[i].Name.Returns(name);
            clients[i].ExistsAsync(tokenSource.Token).Returns(Task.FromResult(Response.FromValue(i != j, Substitute.For<Response>())));

            queueServiceClient.GetQueueClient(name).Returns(clients[i]);
        }

        // Test
        var taskHubInfo = new TaskHubInfo { PartitionCount = clients.Length, TaskHubName = TaskHubName };
        var controlQueues = new ControlQueues(queueServiceClient, taskHubInfo);

        Assert.False(await controlQueues.ExistAsync(tokenSource.Token));

        for (int i = 0; i < clients.Length; i++)
        {
            queueServiceClient.Received(i <= j ? 1 : 0).GetQueueClient(ControlQueues.GetName(TaskHubName, i));
            await clients[i].Received(i <= j ? 1 : 0).ExistsAsync(tokenSource.Token);
        }
    }

    [Fact]
    public async Task GivenAvailableQueues_WhenCheckingExistence_ThenReturnTrue()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        using var tokenSource = new CancellationTokenSource();

        QueueServiceClient queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
        var clients = new QueueClient[3];
        for (int i = 0; i < clients.Length; i++)
        {
            string name = ControlQueues.GetName(TaskHubName, i);
            clients[i] = Substitute.For<QueueClient>("UseDevelopmentStorage=true", name);
            clients[i].Name.Returns(name);
            clients[i].ExistsAsync(tokenSource.Token).Returns(Task.FromResult(Response.FromValue(true, Substitute.For<Response>())));

            queueServiceClient.GetQueueClient(name).Returns(clients[i]);
        }

        // Test
        var taskHubInfo = new TaskHubInfo { PartitionCount = clients.Length, TaskHubName = TaskHubName };
        var controlQueues = new ControlQueues(queueServiceClient, taskHubInfo);

        Assert.True(await controlQueues.ExistAsync(tokenSource.Token));

        for (int i = 0; i < clients.Length; i++)
        {
            queueServiceClient.Received(1).GetQueueClient(ControlQueues.GetName(TaskHubName, i));
            await clients[i].Received(1).ExistsAsync(tokenSource.Token);
        }
    }
}
