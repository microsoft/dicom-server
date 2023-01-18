// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using NSubstitute;
using Xunit;
using System.Text.Json;
using System.Net;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.TaskHub;

public class LeasesContainerTests
{
    private readonly string _containerName = LeasesContainer.GetName(TaskHubName);
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>("UseDevelopmentStorage=true");
    private readonly BlobContainerClient _blobContainerClient;
    private readonly BlobClient _blobClient;
    private readonly LeasesContainer _leasesContainer;

    private const string TaskHubName = "TestTaskHub";

    public LeasesContainerTests()
    {
        _blobContainerClient = Substitute.For<BlobContainerClient>("UseDevelopmentStorage=true", _containerName);
        _blobClient = Substitute.For<BlobClient>("UseDevelopmentStorage=true", _containerName, LeasesContainer.TaskHubBlobName);
        _blobServiceClient.GetBlobContainerClient(_containerName).Returns(_blobContainerClient);
        _blobContainerClient.GetBlobClient(LeasesContainer.TaskHubBlobName).Returns(_blobClient);
        _leasesContainer = new LeasesContainer(_blobServiceClient, TaskHubName);
    }

    [Fact]
    public async Task GivenMissingContainerOrBlob_WhenGettingInfo_ThenReturnNull()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();
        _blobClient
            .DownloadContentAsync(tokenSource.Token)
            .Returns(
                Task.FromException<Response<BlobDownloadResult>>(
                    new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found.")));

        // Test
        Assert.Null(await _leasesContainer.GetTaskHubInfoAsync(tokenSource.Token));

        _blobServiceClient.Received(1).GetBlobContainerClient(_containerName);
        _blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        await _blobClient.Received(1).DownloadContentAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableBlob_WhenGettingInfo_ThenReturnObject()
    {
        // Set up clients
        using var tokenSource = new CancellationTokenSource();

        var expected = new TaskHubInfo { CreatedAt = DateTime.UtcNow, PartitionCount = 7, TaskHubName = TaskHubName };
        BlobDownloadResult result = BlobDownloadResultFactory.CreateResult(new BinaryData(JsonSerializer.Serialize(expected)));
        _blobClient
            .DownloadContentAsync(tokenSource.Token)
            .Returns(Response.FromValue(result, Substitute.For<Response>()));

        // Test
        TaskHubInfo actual = await _leasesContainer.GetTaskHubInfoAsync(tokenSource.Token);

        Assert.NotNull(actual);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.PartitionCount, actual.PartitionCount);
        Assert.Equal(expected.TaskHubName, actual.TaskHubName);

        _blobServiceClient.Received(1).GetBlobContainerClient(_containerName);
        _blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        await _blobClient.Received(1).DownloadContentAsync(tokenSource.Token);
    }
}
