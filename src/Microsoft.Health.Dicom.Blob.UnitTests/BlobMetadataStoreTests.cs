// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests;
public class BlobMetadataStoreTests
{
    private readonly BlobMetadataStore _blobMetadataStore;
    private readonly DicomFileNameWithPrefix _nameWithPrefix;

    public BlobMetadataStoreTests()
    {
        var blobContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
        var metadataContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };

        IOptionsMonitor<BlobContainerConfiguration> optionsMonitor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
        optionsMonitor.Get(Constants.BlobContainerConfigurationName).Returns(blobContainerConfiguration);
        optionsMonitor.Get(Constants.MetadataContainerConfigurationName).Returns(metadataContainerConfiguration);
        var blobClient = new BlobServiceClient("UseDevelopmentStorage=true");
        blobClient.CreateBlobContainer(blobContainerConfiguration.ContainerName);
        blobClient.CreateBlobContainer(metadataContainerConfiguration.ContainerName);

        _nameWithPrefix = Substitute.For<DicomFileNameWithPrefix>();
        _blobMetadataStore = new BlobMetadataStore(
            blobClient,
            new RecyclableMemoryStreamManager(),
            _nameWithPrefix,
            optionsMonitor,
            Options.Create(new JsonSerializerOptions()),
            new BlobStoreMeter(),
            new BlobRetrieveMeter(),
            NullLogger<BlobMetadataStore>.Instance);
    }

    [Fact]
    public async Task GivenFileIdentifier_WhenGetInstanceFramesRangeWithInvalidVersion_ShouldThrowExceptionAndAttemptTwice()
    {
        await Assert.ThrowsAsync<ItemNotFoundException>(async () => await _blobMetadataStore.GetInstanceFramesRangeAsync(1, CancellationToken.None));
        _nameWithPrefix.Received(1).GetInstanceFramesRangeFileNameWithSpace(1);
        _nameWithPrefix.Received(1).GetInstanceFramesRangeFileName(1);
    }
}
