// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Identity;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.TableStorage.Configs;
using Microsoft.Health.DicomCast.TableStorage.Features.Health;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.TableStorage;

public static class DicomCastTableRegistrationExtension
{
    public const string TableStoreConfigurationSectionName = "TableStore";

    /// <summary>
    /// Adds the table data store for dicom cast.
    /// </summary>
    /// <param name="serviceCollection">Service collection</param>
    /// <param name="configuration">The configuration for the server.</param>
    /// <param name="configureAction">An optional delegate to set <see cref="TableDataStoreConfiguration"/> properties after values have been loaded from configuration.</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddTableStorageDataStore(this IServiceCollection serviceCollection, IConfiguration configuration, Action<TableDataStoreConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        serviceCollection
                .AddTableDataStore(configuration, configureAction)
                .AddHealthChecks().AddCheck<TableHealthCheck>(name: nameof(TableHealthCheck));

        serviceCollection.Replace(new ServiceDescriptor(typeof(IExceptionStore), typeof(TableExceptionStore), ServiceLifetime.Singleton));

        return serviceCollection;
    }

    private static IServiceCollection AddTableDataStore(this IServiceCollection serviceCollection, IConfiguration configuration, Action<TableDataStoreConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        TableDataStoreConfiguration tableDataStoreConfiguration = RegisterTableDataStoreConfiguration(serviceCollection, configuration, configureAction);

        serviceCollection.AddAzureClients(builder =>
        {
            if (string.IsNullOrWhiteSpace(tableDataStoreConfiguration.ConnectionString))
            {
                builder.AddTableServiceClient(tableDataStoreConfiguration.EndpointUri)
                    .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = tableDataStoreConfiguration.ClientId }));
            }
            else
            {
                builder.AddTableServiceClient(tableDataStoreConfiguration.ConnectionString);
            }
        });

        serviceCollection.Add<TableServiceClientProvider>()
            .Singleton()
            .AsSelf()
            .AsService<IHostedService>()
            .AsService<IRequireInitializationOnFirstRequest>();

        serviceCollection.Add<TableServiceClientInitializer>()
            .Singleton()
            .AsService<ITableServiceClientInitializer>();

        serviceCollection.Add<TableExceptionStore>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        serviceCollection.Add<TableSyncStateStore>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        return serviceCollection;
    }

    private static TableDataStoreConfiguration RegisterTableDataStoreConfiguration(IServiceCollection serviceCollection, IConfiguration configuration, Action<TableDataStoreConfiguration> configureAction = null)
    {
        var tableDataStoreConfiguration = new TableDataStoreConfiguration();
        configuration.GetSection(TableStoreConfigurationSectionName).Bind(tableDataStoreConfiguration);

        configureAction?.Invoke(tableDataStoreConfiguration);

        if (string.IsNullOrEmpty(tableDataStoreConfiguration.ConnectionString) && tableDataStoreConfiguration.EndpointUri == null)
        {
            tableDataStoreConfiguration.ConnectionString = TableStorageLocalEmulator.ConnectionString;
        }

        serviceCollection.AddSingleton(Options.Create(tableDataStoreConfiguration));
        return tableDataStoreConfiguration;
    }
}
