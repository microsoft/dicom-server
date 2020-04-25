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
    public class DicomDeleteServiceTestsFixture : IAsyncLifetime
    {
        private readonly DicomSqlDataStoreTestsFixture _dicomSqlDataStoreTestsFixture;
        private readonly DicomDataStoreTestsFixture _dicomBlobStorageTestsFixture;

        public DicomDeleteServiceTestsFixture()
        {
            _dicomSqlDataStoreTestsFixture = new DicomSqlDataStoreTestsFixture();
            _dicomBlobStorageTestsFixture = new DicomDataStoreTestsFixture();

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public DicomDeleteService DicomDeleteService { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public IDicomIndexDataStore DicomIndexDataStore => _dicomSqlDataStoreTestsFixture.DicomIndexDataStore;

        public IDicomIndexDataStoreTestHelper DicomIndexDataStoreTestHelper => _dicomSqlDataStoreTestsFixture.TestHelper;

        public IDicomFileStore DicomFileStore => _dicomBlobStorageTestsFixture.DicomFileStore;

        public IDicomMetadataStore DicomMetadataStore => _dicomBlobStorageTestsFixture.DicomMetadataStore;

        public async Task InitializeAsync()
        {
            await _dicomSqlDataStoreTestsFixture.InitializeAsync();
            await _dicomBlobStorageTestsFixture.InitializeAsync();

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
            DicomDeleteService = new DicomDeleteService(
                _dicomSqlDataStoreTestsFixture.DicomIndexDataStore,
                _dicomBlobStorageTestsFixture.DicomMetadataStore,
                _dicomBlobStorageTestsFixture.DicomFileStore,
                optionsConfiguration,
                _dicomSqlDataStoreTestsFixture.SqlTransactionHandler,
                NullLogger<DicomDeleteService>.Instance);
        }

        public async Task DisposeAsync()
        {
            await _dicomSqlDataStoreTestsFixture.DisposeAsync();
            await _dicomBlobStorageTestsFixture.DisposeAsync();
        }
    }
}
