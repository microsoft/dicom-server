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
using Microsoft.IO;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;
using BlobConstants = Microsoft.Health.Dicom.Blob.Constants;
using MetadataConstants = Microsoft.Health.Dicom.Metadata.Constants;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomBlobStorageTestsFixture : IAsyncLifetime
    {
        private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration;
        private readonly BlobContainerConfiguration _blobContainerConfiguration;
        private readonly BlobContainerConfiguration _metadataContainerConfiguration;
        private CloudBlobClient _blobClient;

        public DicomBlobStorageTestsFixture()
        {
            _blobContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _metadataContainerConfiguration = new BlobContainerConfiguration { ContainerName = Guid.NewGuid().ToString() };
            _blobDataStoreConfiguration = new BlobDataStoreConfiguration
            {
                ConnectionString = Environment.GetEnvironmentVariable("Blob:ConnectionString") ?? BlobLocalEmulator.ConnectionString,
            };
            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public IDicomBlobDataStore DicomBlobDataStore { get; private set; }

        public IDicomMetadataStore DicomMetadataStore { get; private set; }

        public IDicomInstanceMetadataStore DicomInstanceMetadataStore { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public async Task InitializeAsync()
        {
            IOptionsMonitor<BlobContainerConfiguration> optionsMonitor = Substitute.For<IOptionsMonitor<BlobContainerConfiguration>>();
            optionsMonitor.Get(BlobConstants.ContainerConfigurationName).Returns(_blobContainerConfiguration);
            optionsMonitor.Get(MetadataConstants.ContainerConfigurationName).Returns(_metadataContainerConfiguration);

            IBlobClientTestProvider testProvider = new BlobClientReadWriteTestProvider(RecyclableMemoryStreamManager);

            var blobClientInitializer = new BlobClientInitializer(testProvider, NullLogger<BlobClientInitializer>.Instance);
            _blobClient = blobClientInitializer.CreateBlobClient(_blobDataStoreConfiguration);

            var blobContainerInitializer = new BlobContainerInitializer(_blobContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);
            var metadataContainerInitializer = new BlobContainerInitializer(_metadataContainerConfiguration.ContainerName, NullLogger<BlobContainerInitializer>.Instance);

            await blobClientInitializer.InitializeDataStoreAsync(
                                            _blobClient,
                                            _blobDataStoreConfiguration,
                                            new List<IBlobContainerInitializer> { blobContainerInitializer, metadataContainerInitializer });

            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());

            DicomBlobDataStore = new DicomBlobDataStore(_blobClient, optionsMonitor, NullLogger<DicomBlobDataStore>.Instance);
            DicomMetadataStore = new DicomMetadataStore(_blobClient, optionsMonitor, new DicomMetadataConfiguration(), NullLogger<DicomMetadataStore>.Instance);
            DicomInstanceMetadataStore = new DicomInstanceMetadataStore(_blobClient, jsonSerializer, optionsMonitor, RecyclableMemoryStreamManager, NullLogger<DicomInstanceMetadataStore>.Instance);
        }

        public async Task DisposeAsync()
        {
            using (_blobClient as IDisposable)
            {
                CloudBlobContainer blobContainer = _blobClient.GetContainerReference(_blobContainerConfiguration.ContainerName);
                await blobContainer.DeleteIfExistsAsync();

                CloudBlobContainer metadataContainer = _blobClient.GetContainerReference(_metadataContainerConfiguration.ContainerName);
                await metadataContainer.DeleteIfExistsAsync();
            }
        }
    }
}
