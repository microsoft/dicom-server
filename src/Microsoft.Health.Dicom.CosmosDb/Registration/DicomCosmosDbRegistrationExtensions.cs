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
using Microsoft.Health.CosmosDb.Features.Storage.Versioning;
using Microsoft.Health.Dicom.CosmosDb;
using Microsoft.Health.Dicom.CosmosDb.Features.Health;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage.Versioning;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomCosmosDbRegistrationExtensions
    {
        public static IServiceCollection AddDicomCosmosDb(this IServiceCollection services, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            services.AddCosmosDb();

            services
                .Configure<CosmosCollectionConfiguration>(
                    Constants.CollectionConfigurationName,
                    cosmosCollectionConfiguration => configuration.GetSection("DicomWeb:CosmosDb")
                .Bind(cosmosCollectionConfiguration));

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

            services.AddHealthChecks()
                .AddCheck<DicomCosmosHealthCheck>(name: nameof(DicomCosmosHealthCheck));

            return services;
        }
    }
}
