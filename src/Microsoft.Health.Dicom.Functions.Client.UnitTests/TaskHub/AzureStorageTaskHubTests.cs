// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class AzureStorageTaskHubTests
{
    private readonly ControlQueueCollection _controlQueues = Substitute.For<ControlQueueCollection>(Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true"), new TaskHubInfo());
    private readonly WorkItemQueue _workItemQueue = Substitute.For<WorkItemQueue>(Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true"), "Foo");
    private readonly InstanceTable _instanceTable = Substitute.For<InstanceTable>(Substitute.For<TableServiceClient>("UseDevelopmentStorage=true"), "Foo");
    private readonly HistoryTable _historyTable = Substitute.For<HistoryTable>(Substitute.For<TableServiceClient>("UseDevelopmentStorage=true"), "Foo");
    private readonly AzureStorageTaskHub _taskHub;

    public AzureStorageTaskHubTests()
    {
        _taskHub = new AzureStorageTaskHub(
            _controlQueues,
            _workItemQueue,
            _instanceTable,
            _historyTable,
            NullLogger<AzureStorageTaskHub>.Instance);
    }

    [Fact]
    public async Task GivenMissingControlQueues_WhenCheckingHealth_ThenReturnFalse()
    {
        using var tokenSource = new CancellationTokenSource();

        _controlQueues.ExistAsync(tokenSource.Token).Returns(false);

        Assert.False(await _taskHub.IsReadyAsync(tokenSource.Token));

        await _controlQueues.Received(1).ExistAsync(tokenSource.Token);
        await _workItemQueue.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _instanceTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _historyTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMissingWorkItemQueue_WhenCheckingHealth_ThenReturnFalse()
    {
        using var tokenSource = new CancellationTokenSource();

        _controlQueues.ExistAsync(tokenSource.Token).Returns(true);
        _workItemQueue.ExistsAsync(tokenSource.Token).Returns(false);

        Assert.False(await _taskHub.IsReadyAsync(tokenSource.Token));

        await _controlQueues.Received(1).ExistAsync(tokenSource.Token);
        await _workItemQueue.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _instanceTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _historyTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMissingInstanceTable_WhenCheckingHealth_ThenReturnFalse()
    {
        using var tokenSource = new CancellationTokenSource();

        _controlQueues.ExistAsync(tokenSource.Token).Returns(true);
        _workItemQueue.ExistsAsync(tokenSource.Token).Returns(true);
        _instanceTable.ExistsAsync(tokenSource.Token).Returns(false);

        Assert.False(await _taskHub.IsReadyAsync(tokenSource.Token));

        await _controlQueues.Received(1).ExistAsync(tokenSource.Token);
        await _workItemQueue.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _instanceTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _historyTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMissingHistoryTable_WhenCheckingHealth_ThenReturnFalse()
    {
        using var tokenSource = new CancellationTokenSource();

        _controlQueues.ExistAsync(tokenSource.Token).Returns(true);
        _workItemQueue.ExistsAsync(tokenSource.Token).Returns(true);
        _instanceTable.ExistsAsync(tokenSource.Token).Returns(true);
        _historyTable.ExistsAsync(tokenSource.Token).Returns(false);

        Assert.False(await _taskHub.IsReadyAsync(tokenSource.Token));

        await _controlQueues.Received(1).ExistAsync(tokenSource.Token);
        await _workItemQueue.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _instanceTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _historyTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenAvailableTaskHub_WhenCheckingHealth_ThenReturnTrue()
    {
        using var tokenSource = new CancellationTokenSource();

        _controlQueues.ExistAsync(tokenSource.Token).Returns(true);
        _workItemQueue.ExistsAsync(tokenSource.Token).Returns(true);
        _instanceTable.ExistsAsync(tokenSource.Token).Returns(true);
        _historyTable.ExistsAsync(tokenSource.Token).Returns(true);

        Assert.True(await _taskHub.IsReadyAsync(tokenSource.Token));

        await _controlQueues.Received(1).ExistAsync(tokenSource.Token);
        await _workItemQueue.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _instanceTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _historyTable.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }
}
