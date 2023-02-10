// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class AzureStorageTaskHubClientTests
{
    private const string TaskHubName = "TestTaskHub";
    private readonly LeasesContainer _leasesContainer = Substitute.For<LeasesContainer>(Substitute.For<BlobServiceClient>("UseDevelopmentStorage=true"), "Foo");
    private readonly AzureStorageTaskHubClient _client;

    public AzureStorageTaskHubClientTests()
    {
        _client = new AzureStorageTaskHubClient(
            TaskHubName,
            _leasesContainer,
            Substitute.For<QueueServiceClient>("UseDevelopmentStorage=true"),
            Substitute.For<TableServiceClient>("UseDevelopmentStorage=true"),
            NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task GivenMissingLeases_WhenGettingTaskHub_ThenReturnNull()
    {
        using var tokenSource = new CancellationTokenSource();

        _leasesContainer.GetTaskHubInfoAsync(tokenSource.Token).Returns((TaskHubInfo)null);

        Assert.Null(await _client.GetTaskHubAsync(tokenSource.Token));

        await _leasesContainer.Received(1).GetTaskHubInfoAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableLeases_WhenGettingTaskHub_ThenReturnObject()
    {
        using var tokenSource = new CancellationTokenSource();
        var taskHubInfo = new TaskHubInfo { PartitionCount = 4, TaskHubName = TaskHubName };

        _leasesContainer.GetTaskHubInfoAsync(tokenSource.Token).Returns(taskHubInfo);

        Assert.IsType<AzureStorageTaskHub>(await _client.GetTaskHubAsync(tokenSource.Token));

        await _leasesContainer.Received(1).GetTaskHubInfoAsync(tokenSource.Token);
    }
}
