// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.DicomTests.Integration.Persistence
{
    public class DicomBlobStorageTestsFixture : IAsyncLifetime
    {
        private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration;
        private readonly BlobContainerConfiguration _blobContainerConfiguration;
        private CloudBlobClient _blobClient;

        public DicomBlobStorageTestsFixture()
        {
            _blobContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _blobDataStoreConfiguration = new BlobDataStoreConfiguration
            {
                ConnectionString = Environment.GetEnvironmentVariable("Blob:ConnectionString") ?? BlobLocalEmulator.ConnectionString,
            };
        }

        public IDicomBlobDataStore DicomBlobDataStore { get; private set; }

        public async Task InitializeAsync()
        {
            IOptionsMonitor<BlobContainerConfiguration> optionsMonitor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
            optionsMonitor.Get(Constants.ContainerConfigurationName).Returns(_blobContainerConfiguration);

            IBlobClientTestProvider testProvider = new BlobClientReadWriteTestProvider();

            var blobClientInitializer = new BlobClientInitializer(testProvider, NullLogger<BlobClientInitializer>.Instance);
            _blobClient = blobClientInitializer.CreateBlobClient(_blobDataStoreConfiguration);

            var blobContainerInitializer = new BlobContainerInitializer(_blobContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);

            await blobClientInitializer.InitializeDataStoreAsync(
                                            _blobClient,
                                            _blobDataStoreConfiguration,
                                            new List<IBlobContainerInitializer> { blobContainerInitializer });

            DicomBlobDataStore = new DicomBlobDataStore(_blobClient, optionsMonitor, NullLogger<DicomBlobDataStore>.Instance);
        }

        public async Task DisposeAsync()
        {
            using (_blobClient as IDisposable)
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(_blobContainerConfiguration.ContainerName);
                await container.DeleteIfExistsAsync();
            }
        }
    }
}
