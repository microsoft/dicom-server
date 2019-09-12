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
using Microsoft.Health.Dicom.Core.Features.Transaction;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Microsoft.Health.Dicom.Transactional.Features.Storage;
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
        private CloudBlobClient _cloudBlobClient;

        public DicomBlobStorageTestsFixture()
        {
            _blobContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _metadataContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _transactionalContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _blobDataStoreConfiguration = new BlobDataStoreConfiguration
            {
                ConnectionString = Environment.GetEnvironmentVariable("Blob:ConnectionString") ?? BlobLocalEmulator.ConnectionString,
            };
        }

        public IDicomBlobDataStore DicomBlobDataStore { get; private set; }

        public IDicomMetadataStore DicomMetadataStore { get; private set; }

        public IDicomInstanceMetadataStore DicomInstanceMetadataStore { get; private set; }

        public IDicomTransactionService DicomTransactionService { get; private set; }

        public async Task InitializeAsync()
        {
            IOptionsMonitor<BlobContainerConfiguration> optionsMonitor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
            optionsMonitor.Get(BlobConstants.ContainerConfigurationName).Returns(_blobContainerConfiguration);
            optionsMonitor.Get(MetadataConstants.ContainerConfigurationName).Returns(_metadataContainerConfiguration);
            optionsMonitor.Get(TransactionalConstants.ContainerConfigurationName).Returns(_transactionalContainerConfiguration);

            IBlobClientTestProvider testProvider = new BlobClientReadWriteTestProvider();

            var blobClientInitializer = new BlobClientInitializer(testProvider, NullLogger<BlobClientInitializer>.Instance);
            _cloudBlobClient = blobClientInitializer.CreateBlobClient(_blobDataStoreConfiguration);

            var blobContainerInitializer = new BlobContainerInitializer(_blobContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);
            var metadataContainerInitializer = new BlobContainerInitializer(_metadataContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);
            var transactionalContainerInitializer = new BlobContainerInitializer(_transactionalContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);

            await blobClientInitializer.InitializeDataStoreAsync(
                                            _cloudBlobClient,
                                            _blobDataStoreConfiguration,
                                            new List<IBlobContainerInitializer> { blobContainerInitializer, metadataContainerInitializer, transactionalContainerInitializer });

            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());

            DicomBlobDataStore = new DicomBlobDataStore(_cloudBlobClient, optionsMonitor, NullLogger<DicomBlobDataStore>.Instance);
            DicomMetadataStore = new DicomMetadataStore(_cloudBlobClient, optionsMonitor, new DicomMetadataConfiguration(), NullLogger<DicomMetadataStore>.Instance);
            DicomInstanceMetadataStore = new DicomInstanceMetadataStore(_cloudBlobClient, jsonSerializer, optionsMonitor, NullLogger<DicomInstanceMetadataStore>.Instance);
            DicomTransactionService = new DicomTransactionService(_cloudBlobClient, optionsMonitor, NullLogger<DicomTransactionService>.Instance);
        }

        public async Task DisposeAsync()
        {
            using (_cloudBlobClient as IDisposable)
            {
                CloudBlobContainer blobContainer = _cloudBlobClient.GetContainerReference(_blobContainerConfiguration.ContainerName);
                await blobContainer.DeleteIfExistsAsync();

                CloudBlobContainer metadataContainer = _cloudBlobClient.GetContainerReference(_metadataContainerConfiguration.ContainerName);
                await metadataContainer.DeleteIfExistsAsync();
            }
        }
    }
}
