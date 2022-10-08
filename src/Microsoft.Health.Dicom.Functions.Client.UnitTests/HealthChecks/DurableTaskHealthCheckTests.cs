// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.HealthChecks;

public class DurableTaskHealthCheckTests
{
    private readonly CloudBlobClient _blobClient = Substitute.For<CloudBlobClient>(new Uri("http://127.0.0.1:10000/devstoreaccount1"));
    private readonly CloudQueueClient _queueClient = Substitute.For<CloudQueueClient>(new Uri("http://127.0.0.1:10001/devstoreaccount1"), new StorageCredentials());
    private readonly CloudTableClient _tableClient = Substitute.For<CloudTableClient>(new Uri("http://127.0.0.1:10002/devstoreaccount1"), new StorageCredentials());
    private readonly DurableTaskHealthCheck _healthCheck;

    public DurableTaskHealthCheckTests()
    {
        _healthCheck = new DurableTaskHealthCheck("TestHub", _blobClient, _queueClient, _tableClient, NullLogger<DurableTaskHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenCannotConnectToTaskHubBlob_ThenThrowException()
    {
        using var tokenSource = new CancellationTokenSource();

        _blobClient.ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromException<ContainerResultSegment>(new IOException()));
        _queueClient.ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<QueueResultSegment>(null));
        _tableClient.ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<TableResultSegment>(null));

        await Assert.ThrowsAsync<IOException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));

        await _blobClient.Received(1).ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _queueClient.Received(0).ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _tableClient.Received(0).ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenCannotConnectToTaskHubQueue_ThenThrowException()
    {
        using var tokenSource = new CancellationTokenSource();

        _blobClient.ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<ContainerResultSegment>(null));
        _queueClient.ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromException<QueueResultSegment>(new IOException()));
        _tableClient.ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<TableResultSegment>(null));

        await Assert.ThrowsAsync<IOException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));

        await _blobClient.Received(1).ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _queueClient.Received(1).ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _tableClient.Received(0).ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenCannotConnectToTaskHubTable_ThenThrowException()
    {
        using var tokenSource = new CancellationTokenSource();

        _blobClient.ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<ContainerResultSegment>(null));
        _queueClient.ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<QueueResultSegment>(null));
        _tableClient.ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token).Returns(Task.FromException<TableResultSegment>(new IOException()));

        await Assert.ThrowsAsync<IOException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));

        await _blobClient.Received(1).ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _queueClient.Received(1).ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _tableClient.Received(1).ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token);
    }

    [Fact]
    public async Task GivenHealthCheck_WhenCanConnectToTaskHub_ThenReturnHealthy()
    {
        using var tokenSource = new CancellationTokenSource();

        _blobClient.ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<ContainerResultSegment>(null));
        _queueClient.ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<QueueResultSegment>(null));
        _tableClient.ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token).Returns(Task.FromResult<TableResultSegment>(null));

        HealthCheckResult actual = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token);

        await _blobClient.Received(1).ListContainersSegmentedAsync(null, ContainerListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _queueClient.Received(1).ListQueuesSegmentedAsync(null, QueueListingDetails.None, 1, null, null, null, tokenSource.Token);
        await _tableClient.Received(1).ListTablesSegmentedAsync(null, 1, null, null, null, tokenSource.Token);

        Assert.Equal(HealthStatus.Healthy, actual.Status);
    }
}
