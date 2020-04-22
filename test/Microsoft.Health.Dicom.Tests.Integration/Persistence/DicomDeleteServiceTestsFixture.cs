// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        private readonly DicomSqlIndexDataStoreTestsFixture _dicomSqlIndexDataStoreTestsFixture;
        private readonly DicomDataStoreTestsFixture _dicomBlobStorageTestsFixture;

        public DicomDeleteServiceTestsFixture()
        {
            _dicomSqlIndexDataStoreTestsFixture = new DicomSqlIndexDataStoreTestsFixture();
            _dicomBlobStorageTestsFixture = new DicomDataStoreTestsFixture();

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public DicomDeleteService DicomDeleteService { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public IDicomIndexDataStore DicomIndexDataStore => _dicomSqlIndexDataStoreTestsFixture.DicomIndexDataStore;

        public IDicomIndexDataStoreTestHelper DicomIndexDataStoreTestHelper => _dicomSqlIndexDataStoreTestsFixture.TestHelper;

        public IDicomFileStore DicomFileStore => _dicomBlobStorageTestsFixture.DicomFileStore;

        public IDicomMetadataStore DicomMetadataStore => _dicomBlobStorageTestsFixture.DicomMetadataStore;

        public async Task InitializeAsync()
        {
            await _dicomSqlIndexDataStoreTestsFixture.InitializeAsync();
            await _dicomBlobStorageTestsFixture.InitializeAsync();

            var cleanupConfiguration = new DeletedInstanceCleanupConfiguration
            {
                BatchSize = 10,
                DeleteDelay = 1,
                MaxRetries = 3,
                PollingInterval = 1,
                RetryBackOff = 2,
            };

            var optionsConfiguration = Substitute.For<IOptions<DeletedInstanceCleanupConfiguration>>();
            optionsConfiguration.Value.Returns(cleanupConfiguration);
            DicomDeleteService = new DicomDeleteService(
                _dicomSqlIndexDataStoreTestsFixture.DicomIndexDataStore,
                _dicomBlobStorageTestsFixture.DicomMetadataStore,
                _dicomBlobStorageTestsFixture.DicomFileStore,
                optionsConfiguration,
                _dicomSqlIndexDataStoreTestsFixture.SqlTransactionHandler,
                NullLogger<DicomDeleteService>.Instance);
        }

        public async Task DisposeAsync()
        {
            await _dicomSqlIndexDataStoreTestsFixture.DisposeAsync();
            await _dicomBlobStorageTestsFixture.DisposeAsync();
        }
    }
}
