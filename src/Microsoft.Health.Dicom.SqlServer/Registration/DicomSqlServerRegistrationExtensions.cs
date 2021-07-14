// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Store;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Registration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomSqlServerRegistrationExtensions
    {
        public static IDicomServerBuilder AddSqlServer(
            this IDicomServerBuilder dicomServerBuilder,
            IConfiguration configurationRoot,
            Action<SqlServerDataStoreConfiguration> configureAction = null)
        {
            IServiceCollection services = EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder)).Services;

            // Add core SQL services
            services
                .AddSqlServerConnection(
                    config =>
                    {
                        configurationRoot?.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(config);
                        configureAction?.Invoke(config);
                    })
                .AddSqlServerManagement<SchemaVersion>()
                .AddSqlServerApi()
                .AddBackgroundSqlSchemaVersionResolver();

            // Add SQL-specific implementations
            services
                .AddSqlChangeFeedStore()
                .AddSqlIndexDataStores()
                .AddSqlQueryStore()
                .AddSqlInstanceStores()
                .AddSqlExtendedQueryTagStores();

            return dicomServerBuilder;
        }

        public static IDicomFunctionsBuilder AddSqlServer(
            this IDicomFunctionsBuilder dicomFunctionsBuilder,
            Action<SqlServerDataStoreConfiguration> configureAction)
        {
            EnsureArg.IsNotNull(dicomFunctionsBuilder, nameof(dicomFunctionsBuilder));
            EnsureArg.IsNotNull(configureAction, nameof(configureAction));

            IServiceCollection services = dicomFunctionsBuilder.Services;

            // Add core SQL services
            services
                .AddSqlServerConnection(configureAction)
                .AddForegroundSqlSchemaVersionResolver();

            // Add SQL-specific implementations
            services
                .AddSqlInstanceStores()
                .AddSqlExtendedQueryTagStores();

            return dicomFunctionsBuilder;
        }

        private static IServiceCollection AddBackgroundSqlSchemaVersionResolver(this IServiceCollection services)
        {
            services.Add(provider => new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max))
                .Singleton()
                .AsSelf();

            services.Add<BackgroundSchemaVersionResolver>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddForegroundSqlSchemaVersionResolver(this IServiceCollection services)
        {
            services.Add<SqlSchemaVersionResolver>()
                .Singleton()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddSqlChangeFeedStore(this IServiceCollection services)
        {
            services.Add<SqlChangeFeedStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddSqlQueryStore(this IServiceCollection services)
        {
            services.Add<SqlQueryStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddSqlExtendedQueryTagStores(this IServiceCollection services)
        {
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

            services.Add<SqlExtendedQueryTagStoreV4>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlStoreFactory<ISqlExtendedQueryTagStore, IExtendedQueryTagStore>>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddSqlInstanceStores(this IServiceCollection services)
        {
            services.Add<SqlInstanceStoreV1>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlInstanceStoreV2>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlInstanceStoreV3>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlInstanceStoreV4>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlStoreFactory<ISqlInstanceStore, IInstanceStore>>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }

        private static IServiceCollection AddSqlIndexDataStores(this IServiceCollection services)
        {
            services.Add<SqlIndexDataStoreV1>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlIndexDataStoreV2>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlIndexDataStoreV3>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlIndexDataStoreV4>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlStoreFactory<ISqlIndexDataStore, IIndexDataStore>>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<ISqlIndexDataStore, SqlLoggingIndexDataStore>();

            return services;
        }
    }
}
