// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Delete;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DeleteServiceTestsFixture : IAsyncLifetime
    {
        private readonly SqlDataStoreTestsFixture _sqlDataStoreTestsFixture;
        private readonly DataStoreTestsFixture _blobStorageTestsFixture;

        public DeleteServiceTestsFixture()
        {
            _sqlDataStoreTestsFixture = new SqlDataStoreTestsFixture();
            _blobStorageTestsFixture = new DataStoreTestsFixture();

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public DeleteService DeleteService { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public IIndexDataStore IndexDataStore => _sqlDataStoreTestsFixture.IndexDataStore;

        public IIndexDataStoreTestHelper IndexDataStoreTestHelper => _sqlDataStoreTestsFixture.TestHelper;

        public IFileStore FileStore => _blobStorageTestsFixture.FileStore;

        public IMetadataStore MetadataStore => _blobStorageTestsFixture.MetadataStore;

        public async Task InitializeAsync()
        {
            await _sqlDataStoreTestsFixture.InitializeAsync();
            await _blobStorageTestsFixture.InitializeAsync();

            var cleanupConfiguration = new DeletedInstanceCleanupConfiguration
            {
                BatchSize = 10,
                DeleteDelay = TimeSpan.FromSeconds(1),
                MaxRetries = 3,
                PollingInterval = TimeSpan.FromSeconds(1),
                RetryBackOff = TimeSpan.FromSeconds(2),
            };

            var optionsConfiguration = Substitute.For<IOptions<DeletedInstanceCleanupConfiguration>>();
            optionsConfiguration.Value.Returns(cleanupConfiguration);
            DeleteService = new DeleteService(
                _sqlDataStoreTestsFixture.IndexDataStoreFactory,
                _blobStorageTestsFixture.MetadataStore,
                _blobStorageTestsFixture.FileStore,
                optionsConfiguration,
                _sqlDataStoreTestsFixture.SqlTransactionHandler,
                NullLogger<DeleteService>.Instance);
        }

        public async Task DisposeAsync()
        {
            await _sqlDataStoreTestsFixture.DisposeAsync();
            await _blobStorageTestsFixture.DisposeAsync();
        }
    }
}
