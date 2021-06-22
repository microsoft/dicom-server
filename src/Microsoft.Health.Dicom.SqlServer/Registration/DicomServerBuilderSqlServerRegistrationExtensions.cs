// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Store;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Registration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderSqlServerRegistrationExtensions
    {
        public static IDicomServerBuilder AddSqlServer(
           this IDicomServerBuilder builder,
           IConfiguration configurationRoot,
           Action<SqlServerDataStoreConfiguration> configureAction = null,
           bool initializeSchema = true)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            IServiceCollection services = builder.Services;
            services.AddSqlServerCommon(configurationRoot, configureAction);
            if (initializeSchema)
            {
                services.AddSqlServerBase<SchemaVersion>(configurationRoot)
                    .AddSqlServerApi();
            }
            else
            {
                services.AddSqlServerBaseWithoutSchemaInitialization();
            }
            return builder;
        }

        private static IServiceCollection AddSqlServerCommon(
            this IServiceCollection services,
            IConfiguration configurationRoot,
            Action<SqlServerDataStoreConfiguration> configureAction)
        {
            var config = new SqlServerDataStoreConfiguration();
            configurationRoot?.GetSection("SqlServer").Bind(config);

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

            services.Add<SqlIndexDataStoreV1>()
                .Scoped()
                .AsImplementedInterfaces();

            services.Add<SqlIndexDataStoreV2>()
                .Scoped()
                .AsImplementedInterfaces();
            services.Add<SqlIndexDataStoreV3>()
                .Scoped()
                .AsImplementedInterfaces();
            services.Add<SqlStoreFactory<ISqlIndexDataStore, IIndexDataStore>>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<ISqlIndexDataStore, SqlLoggingIndexDataStore>();

            services.Add<SqlQueryStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlInstanceStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlChangeFeedStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlExtendedQueryTagStoreV1>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
            services.Add<SqlExtendedQueryTagStoreV2>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
            services.Add<SqlExtendedQueryTagStoreV3>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();
            services.Add<SqlStoreFactory<ISqlExtendedQueryTagStore, IExtendedQueryTagStore>>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddSqlServerBaseWithoutSchemaInitialization(
           this IServiceCollection services)
        {
            // TODO: consider moving these logic into healthcare-shared-components (https://github.com/microsoft/healthcare-shared-components/)
            // once code becomes solid (e.g: merging back to main branch).                 
            services.Add<SqlTransactionHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlConnectionWrapperFactory>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SchemaManagerDataStore>()
               .Singleton()
               .AsSelf()
               .AsImplementedInterfaces();

            // TODO:  Use RetrySqlCommandWrapperFactory instead when moving to healthcare-shared-components 
            services.Add<SqlCommandWrapperFactory>()
                .Singleton()
                .AsSelf();

            services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();

            services.AddSingleton<ISqlConnectionFactory, DefaultSqlConnectionFactory>();

            return services;
        }
    }
}
