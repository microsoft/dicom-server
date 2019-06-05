// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Storage
{
    public class BlobClientInitializerTests
    {
        private const string TestContainerName1 = "testcontainer1";
        private const string TestContainerName2 = "testcontainer2";
        private readonly IBlobClientInitializer _blobClientInitializer;
        private readonly CloudBlobClient _blobClient;
        private readonly IBlobContainerInitializer _containerInitializer1;
        private readonly IBlobContainerInitializer _containerInitializer2;
        private readonly List<IBlobContainerInitializer> _collectionInitializers;
        private readonly CloudBlobContainer _cloudBlobContainer1;
        private readonly CloudBlobContainer _cloudBlobContainer2;
        private readonly BlobDataStoreConfiguration _blobDataStoreConfiguration = new BlobDataStoreConfiguration { };

        public BlobClientInitializerTests()
        {
            _cloudBlobContainer1 = Substitute.For<CloudBlobContainer>(new Uri("https://www.microsoft.com/"));
            _cloudBlobContainer2 = Substitute.For<CloudBlobContainer>(new Uri("https://www.microsoft.com/"));

            IBlobClientTestProvider blobClientTestProvider = Substitute.For<IBlobClientTestProvider>();
            _blobClient = Substitute.For<CloudBlobClient>(new Uri("https://www.microsoft.com/"), null);
            _blobClient.GetContainerReference(TestContainerName1).Returns(_cloudBlobContainer1);
            _blobClient.GetContainerReference(TestContainerName2).Returns(_cloudBlobContainer2);

            _blobClientInitializer = new BlobClientInitializer(blobClientTestProvider, NullLogger<BlobClientInitializer>.Instance);
            _containerInitializer1 = Substitute.For<BlobContainerInitializer>(TestContainerName1, NullLogger<BlobContainerInitializer>.Instance);
            _containerInitializer2 = Substitute.For<BlobContainerInitializer>(TestContainerName2, NullLogger<BlobContainerInitializer>.Instance);
            _collectionInitializers = new List<IBlobContainerInitializer> { _containerInitializer1, _containerInitializer2 };
        }

        [Fact]
        public async void GivenMultipleCollections_WhenInitializing_ThenEachContainerInitializeMethodIsCalled()
        {
            await _blobClientInitializer.InitializeDataStoreAsync(_blobClient, _blobDataStoreConfiguration, _collectionInitializers);

            await _containerInitializer1.Received(1).InitializeContainerAsync(_blobClient);
            await _containerInitializer2.Received(1).InitializeContainerAsync(_blobClient);
        }

        [Fact]
        public async void GivenAConfiguration_WhenInitializing_ThenCreateContainerIfNotExistsIsCalled()
        {
            await _blobClientInitializer.InitializeDataStoreAsync(_blobClient, _blobDataStoreConfiguration, _collectionInitializers);

            await _cloudBlobContainer1.Received(1).CreateIfNotExistsAsync();
            await _cloudBlobContainer2.Received(1).CreateIfNotExistsAsync();
        }

        [Fact]
        public void GivenAnInvalidContainerName_WhenInitializing_ThenCheckExceptionIsThrown()
        {
            string invalidContainerName = "HelloWorld";
            Assert.Throws<ArgumentException>(() => new BlobContainerInitializer(invalidContainerName, NullLogger<BlobContainerInitializer>.Instance));
        }
    }
}
