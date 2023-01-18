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
    private readonly string _queueName = WorkItemQueue.GetName(TaskHubName);
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
    private readonly QueueClient _queueClient;
    private readonly WorkItemQueue _workItemQueue;

    private const string TaskHubName = "TestTaskHub";

    public WorkItemQueueTests()
    {
        _queueClient = Substitute.For<QueueClient>("UseDevelopmentStorage=true", _queueName);
        _queueServiceClient.GetQueueClient(_queueName).Returns(_queueClient);
        _workItemQueue = new WorkItemQueue(_queueServiceClient, TaskHubName);
    }

    [Fact]
    public async Task GivenMissingQueue_WhenCheckingExistence_ThenReturnFalse()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();
        _queueClient
            .ExistsAsync(tokenSource.Token)
            .Returns(Response.FromValue(false, Substitute.For<Response>()));

        // Test
        Assert.False(await _workItemQueue.ExistsAsync(tokenSource.Token));

        _queueServiceClient.Received(1).GetQueueClient(_queueName);
        await _queueClient.Received(1).ExistsAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableQueue_WhenCheckingExistence_ThenReturnTrue()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();
        _queueClient
            .ExistsAsync(tokenSource.Token)
            .Returns(Response.FromValue(true, Substitute.For<Response>()));

        // Test
        Assert.True(await _workItemQueue.ExistsAsync(tokenSource.Token));

        _queueServiceClient.Received(1).GetQueueClient(_queueName);
        await _queueClient.Received(1).ExistsAsync(tokenSource.Token);
    }
}
