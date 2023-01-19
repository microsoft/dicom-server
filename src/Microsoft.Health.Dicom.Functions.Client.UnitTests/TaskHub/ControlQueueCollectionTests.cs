// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class ControlQueueCollectionTests
{
    private readonly TaskHubInfo _taskHubInfo = new TaskHubInfo { PartitionCount = PartitionCount, TaskHubName = TaskHubName };
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true");
    private readonly QueueClient[] _queueClients;
    private readonly ControlQueueCollection _controlQueues;

    private const int PartitionCount = 3;
    private const string TaskHubName = "TestTaskHub";

    public static IEnumerable<object[]> EnumeratePartitions => Enumerable
        .Repeat<object>(null, PartitionCount)
        .Select((obj, i) => new object[] { i });

    public ControlQueueCollectionTests()
    {
        _queueClients = new QueueClient[_taskHubInfo.PartitionCount];
        for (int i = 0; i < _queueClients.Length; i++)
        {
            string name = ControlQueueCollection.GetName(TaskHubName, i);
            _queueClients[i] = Substitute.For<QueueClient>("UseDevelopmentStorage=true", name);
            _queueClients[i].Name.Returns(name);

            _queueServiceClient.GetQueueClient(name).Returns(_queueClients[i]);
        }

        _controlQueues = new ControlQueueCollection(_queueServiceClient, _taskHubInfo);
    }

    [Theory]
    [MemberData(nameof(EnumeratePartitions))]
    public async Task GivenOneMissingQueue_WhenCheckingExistence_ThenReturnFalse(int missingPartitionIndex)
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();

        for (int i = 0; i < _queueClients.Length; i++)
        {
            _queueClients[i]
                .ExistsAsync(tokenSource.Token)
                .Returns(Task.FromResult(Response.FromValue(i != missingPartitionIndex, Substitute.For<Response>())));
        }

        // Test
        Assert.False(await _controlQueues.ExistAsync(tokenSource.Token));

        for (int i = 0; i < _queueClients.Length; i++)
        {
            _queueServiceClient.Received(1).GetQueueClient(ControlQueueCollection.GetName(TaskHubName, i));
            await _queueClients[i].Received(1).ExistsAsync(tokenSource.Token);
        }
    }

    [Fact]
    public async Task GivenAvailableQueues_WhenCheckingExistence_ThenReturnTrue()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();

        for (int i = 0; i < _queueClients.Length; i++)
        {
            _queueClients[i]
                .ExistsAsync(tokenSource.Token)
                .Returns(Task.FromResult(Response.FromValue(true, Substitute.For<Response>())));
        }

        // Test
        Assert.True(await _controlQueues.ExistAsync(tokenSource.Token));

        for (int i = 0; i < _queueClients.Length; i++)
        {
            _queueServiceClient.Received(1).GetQueueClient(ControlQueueCollection.GetName(TaskHubName, i));
            await _queueClients[i].Received(1).ExistsAsync(tokenSource.Token);
        }
    }
}
