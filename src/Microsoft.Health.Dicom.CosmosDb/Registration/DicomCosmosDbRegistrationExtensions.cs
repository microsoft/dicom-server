// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.CosmosDb.Configs;
using Microsoft.Health.CosmosDb.Features.Storage;
using Microsoft.Health.CosmosDb.Features.Storage.StoredProcedures;
using Microsoft.Health.CosmosDb.Features.Storage.Versioning;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.CosmosDb;
using Microsoft.Health.Dicom.CosmosDb.Config;
using Microsoft.Health.Dicom.CosmosDb.Features.Health;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.StoredProcedures;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Versioning;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomCosmosDbRegistrationExtensions
    {
        private static readonly string DicomServerCosmosDbConfigurationSectionName = $"DicomWeb:CosmosDb";

        public static IDicomServerBuilder AddDicomCosmosDbIndexing(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            return serverBuilder
                        .AddCosmosDbIndexingPersistence(configuration)
                        .AddCosmosDbHealthCheck();
        }

        public static IDicomServerBuilder AddCosmosDbIndexingPersistence(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            IServiceCollection services = serverBuilder.Services;

            services.AddCosmosDb();

            services
                .Configure<CosmosCollectionConfiguration>(
                    Constants.CollectionConfigurationName,
                    cosmosCollectionConfiguration => configuration.GetSection(DicomServerCosmosDbConfigurationSectionName)
                .Bind(cosmosCollectionConfiguration));

            // Add the indexing configuration; this is not loaded from the settings configuration for now.
            services.Add<DicomIndexingConfiguration>()
                .Singleton()
                .AsSelf();

            services.Add(sp =>
                {
                    CosmosDataStoreConfiguration config = sp.GetService<CosmosDataStoreConfiguration>();
                    DicomCollectionUpgradeManager upgradeManager = sp.GetService<DicomCollectionUpgradeManager>();
                    ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();
                    IOptionsMonitor<CosmosCollectionConfiguration> namedCosmosCollectionConfiguration = sp.GetService<IOptionsMonitor<CosmosCollectionConfiguration>>();
                    CosmosCollectionConfiguration cosmosCollectionConfiguration = namedCosmosCollectionConfiguration.Get(Constants.CollectionConfigurationName);

                    return new CollectionInitializer(
                        cosmosCollectionConfiguration.CollectionId,
                        config,
                        cosmosCollectionConfiguration.InitialCollectionThroughput,
                        upgradeManager,
                        loggerFactory.CreateLogger<CollectionInitializer>());
                })
                .Singleton()
                .AsService<ICollectionInitializer>();

            services.Add<DicomDocumentClientInitializer>()
                .Singleton()
                .AsService<IDocumentClientInitializer>();

            services.Add<DicomCosmosDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomCollectionUpgradeManager>()
                .Singleton()
                .AsSelf()
                .AsService<IUpgradeManager>();

            services.Add<DicomStoredProcedureInstaller>()
                .Singleton()
                .AsService<IDicomCollectionUpdater>();

            services.TypesInSameAssemblyAs<IDicomStoredProcedure>()
                .AssignableTo<IStoredProcedure>()
                .Singleton()
                .AsSelf()
                .AsService<IDicomStoredProcedure>();

            return serverBuilder;
        }

        private static IDicomServerBuilder AddCosmosDbHealthCheck(this IDicomServerBuilder serverBuilder)
        {
            serverBuilder.Services.AddHealthChecks().AddCheck<DicomCosmosHealthCheck>(name: nameof(DicomCosmosHealthCheck));
            return serverBuilder;
        }
    }
}
