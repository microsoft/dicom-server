// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dicom.Serialization;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;
using BlobConstants = Microsoft.Health.Dicom.Blob.Constants;
using MetadataConstants = Microsoft.Health.Dicom.Metadata.Constants;
using TransactionalConstants = Microsoft.Health.Dicom.Transactional.Constants;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomBlobStorageTestsFixture : IAsyncLifetime
    {
        private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration;
        private readonly BlobContainerConfiguration _blobContainerConfiguration;
        private readonly BlobContainerConfiguration _metadataContainerConfiguration;
        private readonly BlobContainerConfiguration _transactionalContainerConfiguration;

        public DicomBlobStorageTestsFixture()
        {
            _blobContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _metadataContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _transactionalContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _blobDataStoreConfiguration = new BlobDataStoreConfiguration
            {
                ConnectionString = Environment.GetEnvironmentVariable("Blob:ConnectionString") ?? BlobLocalEmulator.ConnectionString,
            };

            OptionsMonitor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
            OptionsMonitor.Get(BlobConstants.ContainerConfigurationName).Returns(_blobContainerConfiguration);
            OptionsMonitor.Get(MetadataConstants.ContainerConfigurationName).Returns(_metadataContainerConfiguration);
            OptionsMonitor.Get(TransactionalConstants.ContainerConfigurationName).Returns(_transactionalContainerConfiguration);
        }

        public IDicomBlobDataStore DicomBlobDataStore { get; private set; }

        public IDicomMetadataStore DicomMetadataStore { get; private set; }

        public IDicomInstanceMetadataStore DicomInstanceMetadataStore { get; private set; }

        public CloudBlobClient CloudBlobClient { get; private set; }

        public IOptionsMonitor<BlobContainerConfiguration> OptionsMonitor { get; }

        public async Task InitializeAsync()
        {
            IBlobClientTestProvider testProvider = new BlobClientReadWriteTestProvider();

            var blobClientInitializer = new BlobClientInitializer(testProvider, NullLogger<BlobClientInitializer>.Instance);
            CloudBlobClient = blobClientInitializer.CreateBlobClient(_blobDataStoreConfiguration);

            var blobContainerInitializer = new BlobContainerInitializer(_blobContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);
            var metadataContainerInitializer = new BlobContainerInitializer(_metadataContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);
            var transactionalContainerInitializer = new BlobContainerInitializer(_transactionalContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);

            await blobClientInitializer.InitializeDataStoreAsync(
                                            CloudBlobClient,
                                            _blobDataStoreConfiguration,
                                            new List<IBlobContainerInitializer> { blobContainerInitializer, metadataContainerInitializer, transactionalContainerInitializer });

            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());

            DicomBlobDataStore = new DicomBlobDataStore(CloudBlobClient, OptionsMonitor, NullLogger<DicomBlobDataStore>.Instance);
            DicomMetadataStore = new DicomMetadataStore(CloudBlobClient, OptionsMonitor, new DicomMetadataConfiguration(), NullLogger<DicomMetadataStore>.Instance);
            DicomInstanceMetadataStore = new DicomInstanceMetadataStore(CloudBlobClient, jsonSerializer, OptionsMonitor, NullLogger<DicomInstanceMetadataStore>.Instance);
        }

        public async Task DisposeAsync()
        {
            using (CloudBlobClient as IDisposable)
            {
                CloudBlobContainer blobContainer = CloudBlobClient.GetContainerReference(_blobContainerConfiguration.ContainerName);
                await blobContainer.DeleteIfExistsAsync();

                CloudBlobContainer metadataContainer = CloudBlobClient.GetContainerReference(_metadataContainerConfiguration.ContainerName);
                await metadataContainer.DeleteIfExistsAsync();
            }
        }
    }
}
