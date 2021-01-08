// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Health;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.TableStorage
{
    public static class DiocmCastTableRegistrationExtension
    {
        public const string TableStoreConfigurationSectionName = "TableStore";

        /// <summary>
        /// Adds the table data store for dicom cast.
        /// </summary>
        /// <param name="serviceCollection">Service collection</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddTableStorageDataStore(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            serviceCollection
                    .AddTableDataStore(configuration)
                    .AddHealthChecks().AddCheck<TableHealthCheck>(name: nameof(TableHealthCheck));

            serviceCollection.Replace(new ServiceDescriptor(typeof(IExceptionStore), typeof(TableExceptionStore), ServiceLifetime.Singleton));

            return serviceCollection;
        }

        private static IServiceCollection AddTableDataStore(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            serviceCollection.Add(provider =>
                {
                    var config = new TableDataStoreConfiguration();
                    configuration.GetSection(TableStoreConfigurationSectionName).Bind(config);

                    if (string.IsNullOrEmpty(config.ConnectionString))
                    {
                        config.ConnectionString = TableStorageLocalEmulator.ConnectionString;
                    }

                    return config;
                })
                .Singleton()
                .AsSelf();

            serviceCollection.Add<TableClientProvider>()
                .Singleton()
                .AsSelf()
                .AsService<IHostedService>()
                .AsService<IRequireInitializationOnFirstRequest>();

            serviceCollection.Add(sp => sp.GetService<TableClientProvider>().CreateTableClient())
                .Singleton()
                .AsSelf();

            serviceCollection.Add<TableClientInitializer>()
                .Singleton()
                .AsService<ITableClientInitializer>();

            serviceCollection.Add<TableStore>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            return serviceCollection;
        }
    }
}
