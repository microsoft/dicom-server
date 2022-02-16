// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Error;
using Microsoft.Health.Dicom.SqlServer.Features.Partition;
using Microsoft.Health.Dicom.SqlServer.Features.Query;
using Microsoft.Health.Dicom.SqlServer.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.SqlServer.Features.Store;
using Microsoft.Health.Dicom.SqlServer.Features.Workitem;
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
                .AddSqlChangeFeedStores()
                .AddSqlExtendedQueryTagStores()
                .AddSqlExtendedQueryTagErrorStores()
                .AddSqlIndexDataStores()
                .AddSqlInstanceStores()
                .AddSqlPartitionStores()
                .AddSqlQueryStores()
                .AddSqlWorkitemStores();

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
                .AddSqlExtendedQueryTagStores()
                .AddSqlExtendedQueryTagErrorStores()
                .AddSqlIndexDataStores()
                .AddSqlInstanceStores()
                .AddSqlPartitionStores();

            return dicomFunctionsBuilder;
        }

        private static IServiceCollection AddBackgroundSqlSchemaVersionResolver(this IServiceCollection services)
        {
            services.Add(provider => new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max))
                .Singleton()
                .AsSelf();

            services.Add<PassthroughSchemaVersionResolver>()
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

        private static IServiceCollection AddSqlChangeFeedStores(this IServiceCollection services)
        {
            services.TryAddScoped<IChangeFeedStore, SqlChangeFeedStore>();
            services.TryAddScoped<VersionedCache<ISqlChangeFeedStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlChangeFeedStore, SqlChangeFeedStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlChangeFeedStore, SqlChangeFeedStoreV6>());

            return services;
        }

        private static IServiceCollection AddSqlExtendedQueryTagStores(this IServiceCollection services)
        {
            services.TryAddScoped<IExtendedQueryTagStore, SqlExtendedQueryTagStore>();
            services.TryAddScoped<VersionedCache<ISqlExtendedQueryTagStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV1>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV2>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV8>());

            return services;
        }

        private static IServiceCollection AddSqlExtendedQueryTagErrorStores(this IServiceCollection services)
        {
            services.TryAddScoped<IExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStore>();
            services.TryAddScoped<VersionedCache<ISqlExtendedQueryTagErrorStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStoreV1>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStoreV6>());

            return services;
        }

        private static IServiceCollection AddSqlIndexDataStores(this IServiceCollection services)
        {
            services.TryAddScoped<IIndexDataStore, SqlIndexDataStore>();
            services.TryAddScoped<VersionedCache<ISqlIndexDataStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV1>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV2>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV3>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV5>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV6>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV10>());

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<ISqlIndexDataStore, SqlLoggingIndexDataStore>();

            return services;
        }

        private static IServiceCollection AddSqlInstanceStores(this IServiceCollection services)
        {
            services.TryAddScoped<IInstanceStore, SqlInstanceStore>();
            services.TryAddScoped<VersionedCache<ISqlInstanceStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV1>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV6>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV10>());
            return services;
        }

        private static IServiceCollection AddSqlPartitionStores(this IServiceCollection services)
        {
            services.TryAddScoped<IPartitionStore, SqlPartitionStore>();
            services.TryAddScoped<VersionedCache<ISqlPartitionStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlPartitionStore, SqlPartitionStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlPartitionStore, SqlPartitionStoreV6>());

            return services;
        }

        private static IServiceCollection AddSqlQueryStores(this IServiceCollection services)
        {
            services.TryAddScoped<IQueryStore, SqlQueryStore>();
            services.TryAddScoped<VersionedCache<ISqlQueryStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlQueryStore, SqlQueryStoreV4>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlQueryStore, SqlQueryStoreV6>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlQueryStore, SqlQueryStoreV9>());

            return services;
        }

        private static IServiceCollection AddSqlWorkitemStores(this IServiceCollection services)
        {
            services.TryAddScoped<IIndexWorkitemStore, SqlWorkitemStore>();
            services.TryAddScoped<VersionedCache<ISqlWorkitemStore>>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlWorkitemStore, SqlWorkitemStoreV9>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlWorkitemStore, SqlWorkitemStoreV11>());

            return services;
        }
    }
}
