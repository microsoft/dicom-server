// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.CosmosDb.Configs;
using Microsoft.Health.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.CosmosDb.Config;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.StoredProcedures;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Versioning;
using Microsoft.Health.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public class DicomCosmosDataStoreTestsFixture : IAsyncLifetime
    {
        private static readonly SemaphoreSlim CollectionInitializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly CosmosDataStoreConfiguration _cosmosDataStoreConfiguration;
        private readonly CosmosCollectionConfiguration _cosmosCollectionConfiguration;
        private readonly DicomIndexingConfiguration _dicomIndexingConfiguration;

        public DicomCosmosDataStoreTestsFixture()
        {
            _cosmosDataStoreConfiguration = new CosmosDataStoreConfiguration
            {
                Host = Environment.GetEnvironmentVariable("CosmosDb:Host") ?? CosmosDbLocalEmulator.Host,
                Key = Environment.GetEnvironmentVariable("CosmosDb:Key") ?? CosmosDbLocalEmulator.Key,
                DatabaseId = Environment.GetEnvironmentVariable("CosmosDb:DatabaseId") ?? "DicomTests",
                AllowDatabaseCreation = true,
                PreferredLocations = Environment.GetEnvironmentVariable("CosmosDb:PreferredLocations")?.Split(';', StringSplitOptions.RemoveEmptyEntries),
            };

            _cosmosCollectionConfiguration = new CosmosCollectionConfiguration
            {
                CollectionId = Guid.NewGuid().ToString(),
            };

            _dicomIndexingConfiguration = new DicomIndexingConfiguration();
        }

        public IDicomIndexDataStore DicomIndexDataStore { get; private set; }

        public IDocumentClient DocumentClient { get; private set; }

        public string DatabaseId => _cosmosDataStoreConfiguration.DatabaseId;

        public string CollectionId => _cosmosCollectionConfiguration.CollectionId;

        public async Task InitializeAsync()
        {
            IOptionsMonitor<CosmosCollectionConfiguration> optionsMonitor = Substitute.For<IOptionsMonitor<CosmosCollectionConfiguration>>();

            optionsMonitor.Get(CosmosDb.Constants.CollectionConfigurationName).Returns(_cosmosCollectionConfiguration);

            var dicomStoredProcs = typeof(IDicomStoredProcedure).Assembly
                .GetTypes()
                .Where(x => !x.IsAbstract && typeof(IDicomStoredProcedure).IsAssignableFrom(x))
                .ToArray()
                .Select(type => (IDicomStoredProcedure)Activator.CreateInstance(type));
            var updaters = new IDicomCollectionUpdater[]
            {
                new DicomStoredProcedureInstaller(dicomStoredProcs),
            };

            var dbLock = new CosmosDbDistributedLockFactory(Substitute.For<Func<IScoped<IDocumentClient>>>(), NullLogger<CosmosDbDistributedLock>.Instance);

            var upgradeManager = new DicomCollectionUpgradeManager(updaters, _cosmosDataStoreConfiguration, optionsMonitor, dbLock, NullLogger<DicomCollectionUpgradeManager>.Instance);
            IDocumentClientTestProvider testProvider = new DocumentClientReadWriteTestProvider();

            var documentClientInitializer = new DicomDocumentClientInitializer(testProvider, NullLogger<DicomDocumentClientInitializer>.Instance);
            DocumentClient = documentClientInitializer.CreateDocumentClient(_cosmosDataStoreConfiguration);
            var collectionInitializer = new CollectionInitializer(_cosmosCollectionConfiguration.CollectionId, _cosmosDataStoreConfiguration, _cosmosCollectionConfiguration.InitialCollectionThroughput, upgradeManager, NullLogger<CollectionInitializer>.Instance);

            // Cosmos DB emulators throws errors when multiple collections are initialized concurrently.
            // Use the semaphore to only allow one initialization at a time.
            await CollectionInitializationSemaphore.WaitAsync();

            try
            {
                await documentClientInitializer.InitializeDataStore(DocumentClient, _cosmosDataStoreConfiguration, new List<ICollectionInitializer> { collectionInitializer });
            }
            finally
            {
                CollectionInitializationSemaphore.Release();
            }

            var documentClient = new NonDisposingScope(DocumentClient);

            DicomIndexDataStore = new DicomCosmosDataStore(
                documentClient,
                _cosmosDataStoreConfiguration,
                optionsMonitor,
                _dicomIndexingConfiguration);
        }

        public async Task DisposeAsync()
        {
            using (DocumentClient as IDisposable)
            {
                await DocumentClient?.DeleteDocumentCollectionAsync(_cosmosDataStoreConfiguration.GetRelativeCollectionUri(_cosmosCollectionConfiguration.CollectionId));
            }
        }
    }
}
