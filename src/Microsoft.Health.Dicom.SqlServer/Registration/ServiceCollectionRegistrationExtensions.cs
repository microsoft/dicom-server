// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.Indexing;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using System;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.SqlServer.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionRegistrationExtensions
    {
        public static IDicomOperationsBuilder AddSqlServer(
            this IDicomOperationsBuilder builder,
            IConfiguration configurationRoot)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            IServiceCollection services = builder.Services;
            SqlServerDataStoreConfiguration config = GetConfiguration(configurationRoot);
            services.AddSingleton(Options.Options.Create(config));

            SchemaInformation schemaInformation = new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max);
            services.AddSingleton(schemaInformation);

            AddInitializedSqlServerBase(services, config);

            services.AddScopedDefault<SqlInstanceStore>();

            services.AddSqlExtendedQueryTagStores();

            services.AddScopedDefault<SqlReindexStore>();
            return builder;
        }

        public static IDicomServerBuilder AddSqlServer(
          this IDicomServerBuilder dicomServerBuilder,
          IConfiguration configurationRoot,
          Action<SqlServerDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
            IServiceCollection services = dicomServerBuilder.Services;

            services.AddSqlServerBase<SchemaVersion>(configurationRoot);
            services.AddSqlServerApi();

            var config = GetConfiguration(configurationRoot);

            services.Add(provider =>
                {
                    configureAction?.Invoke(config);
                    return config;
                })
                .Singleton()
                .AsSelf();

            services.Add(provider => new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max))
                .Singleton()
                .AsSelf();

            services.AddScopedDefault<
                SqlIndexDataStoreV1,
                SqlIndexDataStoreV2,
                SqlIndexDataStoreV3,
                SqlStoreFactory<ISqlIndexDataStore, IIndexDataStore>>();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<ISqlIndexDataStore, SqlLoggingIndexDataStore>();

            services.AddScopedDefault<SqlQueryStore>();

            services.AddScopedDefault<SqlInstanceStore>();

            services.AddScopedDefault<SqlChangeFeedStore>();

            services.AddSqlExtendedQueryTagStores();

            return dicomServerBuilder;
        }

        private static void AddSqlExtendedQueryTagStores(this IServiceCollection services)
        {
            services.AddScopedDefault<
                SqlExtendedQueryTagStoreV1,
                SqlExtendedQueryTagStoreV2,
                SqlExtendedQueryTagStoreV3,
                SqlStoreFactory<ISqlExtendedQueryTagStore, IExtendedQueryTagStore>>();
        }

        private static IServiceCollection AddInitializedSqlServerBase(
           this IServiceCollection services,
           SqlServerDataStoreConfiguration configuration)
        {
            //  SqlServerDataStoreConfiguration is consumed by DefaultSqlConnectionStringProvider
            services.AddSingleton(configuration);

            // TODO: consider moving these logic into healthcare-shared-components (https://github.com/microsoft/healthcare-shared-components/)
            // once code becomes solid (e.g: merging back to main branch).                 
            services.AddScopedDefault<SqlTransactionHandler>();

            services.AddScopedDefault<SqlConnectionWrapperFactory>();

            services.AddSingletonDefault<SchemaManagerDataStore>();

            // TODO:  Use RetrySqlCommandWrapperFactory instead when moving to healthcare-shared-components 
            services.AddSingletonDefault<SqlCommandWrapperFactory>();

            services.AddSingletonDefault<DefaultSqlConnectionStringProvider>();

            services.AddSingletonDefault<DefaultSqlConnectionFactory>();

            return services;
        }

        private static SqlServerDataStoreConfiguration GetConfiguration(IConfiguration configurationRoot)
        {
            var config = new SqlServerDataStoreConfiguration();
            configurationRoot?.GetSection("SqlServer").Bind(config);
            return config;
        }
    }
}
