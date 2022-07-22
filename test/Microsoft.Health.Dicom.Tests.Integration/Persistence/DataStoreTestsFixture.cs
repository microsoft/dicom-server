// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Tests.Common.Serialization;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence;

public class DataStoreTestsFixture : IAsyncLifetime
{
    private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration;
    private readonly BlobContainerConfiguration _blobContainerConfiguration;
    private readonly BlobContainerConfiguration _metadataContainerConfiguration;
    private BlobServiceClient _blobClient;

    public DataStoreTestsFixture()
    {
        IConfiguration environment = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        _blobContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
        _metadataContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
        _blobDataStoreConfiguration = new BlobDataStoreConfiguration
        {
            ConnectionString = environment["BlobStore:ConnectionString"] ?? BlobLocalEmulator.ConnectionString,
        };
        RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public IFileStore FileStore { get; private set; }

    public IMetadataStore MetadataStore { get; private set; }

    public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

    public async Task InitializeAsync()
    {
        IOptionsMonitor<BlobContainerConfiguration> optionsMonitor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
        optionsMonitor.Get(Constants.BlobContainerConfigurationName).Returns(_blobContainerConfiguration);
        optionsMonitor.Get(Constants.MetadataContainerConfigurationName).Returns(_metadataContainerConfiguration);

        IBlobClientTestProvider testProvider = new BlobClientReadWriteTestProvider(RecyclableMemoryStreamManager, NullLogger<BlobClientReadWriteTestProvider>.Instance);

        _blobClient = BlobClientFactory.Create(_blobDataStoreConfiguration);

        var blobClientInitializer = new BlobInitializer(_blobClient, testProvider, NullLogger<BlobInitializer>.Instance);

        var blobContainerInitializer = new BlobContainerInitializer(_blobContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);
        var metadataContainerInitializer = new BlobContainerInitializer(_metadataContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);

        await blobClientInitializer.InitializeDataStoreAsync(
                                        new List<IBlobContainerInitializer> { blobContainerInitializer, metadataContainerInitializer });

        FileStore = new BlobFileStore(_blobClient, Substitute.For<DicomFileNameWithUid>(), Substitute.For<DicomFileNameWithPrefix>(), optionsMonitor, Options.Create(Substitute.For<BlobOperationOptions>()), Options.Create(Substitute.For<BlobMigrationConfiguration>()), NullLogger<BlobFileStore>.Instance);
        MetadataStore = new BlobMetadataStore(_blobClient, RecyclableMemoryStreamManager, Substitute.For<DicomFileNameWithUid>(), Substitute.For<DicomFileNameWithPrefix>(), Options.Create(Substitute.For<BlobMigrationConfiguration>()), optionsMonitor, Options.Create(AppSerializerOptions.Json), NullLogger<BlobMetadataStore>.Instance);
    }

    public async Task DisposeAsync()
    {
        using (_blobClient as IDisposable)
        {
            BlobContainerClient blobContainer = _blobClient.GetBlobContainerClient(_blobContainerConfiguration.ContainerName);
            await blobContainer.DeleteIfExistsAsync();

            BlobContainerClient metadataContainer = _blobClient.GetBlobContainerClient(_metadataContainerConfiguration.ContainerName);
            await metadataContainer.DeleteIfExistsAsync();
        }
    }
}
