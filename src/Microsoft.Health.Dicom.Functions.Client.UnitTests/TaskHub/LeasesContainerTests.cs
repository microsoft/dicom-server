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
    [Fact]
    public async Task GivenMissingContainerOrBlob_WhenGettingInfo_ThenReturnNull()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        string containerName = LeasesContainer.GetName(TaskHubName);
        using var tokenSource = new CancellationTokenSource();

        BlobServiceClient blobServiceClient = Substitute.For<BlobServiceClient>("UseDevelopmentStorage=true");
        BlobContainerClient blobContainerClient = Substitute.For<BlobContainerClient>("UseDevelopmentStorage=true", containerName);
        BlobClient blobClient = Substitute.For<BlobClient>("UseDevelopmentStorage=true", containerName, LeasesContainer.TaskHubBlobName);

        blobServiceClient.GetBlobContainerClient(containerName).Returns(blobContainerClient);
        blobContainerClient.GetBlobClient(LeasesContainer.TaskHubBlobName).Returns(blobClient);
        blobClient
            .DownloadContentAsync(tokenSource.Token)
            .Returns(
                Task.FromException<Response<BlobDownloadResult>>(
                    new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found.")));

        // Test
        var leasesContainer = new LeasesContainer(blobServiceClient, TaskHubName);

        Assert.Null(await leasesContainer.GetTaskHubInfoAsync(tokenSource.Token));

        blobServiceClient.Received(1).GetBlobContainerClient(containerName);
        blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        await blobClient.Received(1).DownloadContentAsync(tokenSource.Token);
    }

    [Fact]
    public async Task GivenAvailableBlob_WhenGettingInfo_ThenReturnObject()
    {
        // Set up clients
        const string TaskHubName = "TestTaskHub";
        string containerName = LeasesContainer.GetName(TaskHubName);
        var expected = new TaskHubInfo { CreatedAt = DateTime.UtcNow, PartitionCount = 7, TaskHubName = TaskHubName };
        using var tokenSource = new CancellationTokenSource();

        BlobServiceClient blobServiceClient = Substitute.For<BlobServiceClient>("UseDevelopmentStorage=true");
        BlobContainerClient blobContainerClient = Substitute.For<BlobContainerClient>("UseDevelopmentStorage=true", containerName);
        BlobClient blobClient = Substitute.For<BlobClient>("UseDevelopmentStorage=true", containerName, LeasesContainer.TaskHubBlobName);
        BlobDownloadResult result = BlobDownloadResultFactory.CreateResult(new BinaryData(JsonSerializer.Serialize(expected)));

        blobServiceClient.GetBlobContainerClient(containerName).Returns(blobContainerClient);
        blobContainerClient.GetBlobClient(LeasesContainer.TaskHubBlobName).Returns(blobClient);
        blobClient.DownloadContentAsync(tokenSource.Token).Returns(Response.FromValue(result, Substitute.For<Response>()));

        // Test
        var leasesContainer = new LeasesContainer(blobServiceClient, TaskHubName);
        TaskHubInfo actual = await leasesContainer.GetTaskHubInfoAsync(tokenSource.Token);

        Assert.NotNull(actual);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.PartitionCount, actual.PartitionCount);
        Assert.Equal(expected.TaskHubName, actual.TaskHubName);

        blobServiceClient.Received(1).GetBlobContainerClient(containerName);
        blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        await blobClient.Received(1).DownloadContentAsync(tokenSource.Token);
    }
}
