// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.SqlServer.Features.ChangeFeed;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag.Errors;
using Microsoft.Health.Dicom.SqlServer.Features.Partitioning;
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

namespace Microsoft.Health.Dicom.SqlServer.Registration;

public static class DicomSqlServerRegistrationExtensions
{
    public static IDicomServerBuilder AddSqlServer(
        this IDicomServerBuilder dicomServerBuilder,
        IConfiguration configurationRoot,
        Action<SqlServerDataStoreConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
        EnsureArg.IsNotNull(configurationRoot, nameof(configurationRoot));

        dicomServerBuilder.Services
            .AddCommonSqlServices(sqlOptions =>
            {
                configurationRoot.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(sqlOptions);
                configureAction?.Invoke(sqlOptions);
            })
            .AddSqlServerManagement<SchemaVersion>()
            .AddSqlServerApi()
            .AddBackgroundSqlSchemaVersionResolver()
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

        dicomFunctionsBuilder.Services
            .AddCommonSqlServices(configureAction)
            .AddForegroundSqlSchemaVersionResolver();

        return dicomFunctionsBuilder;
    }

    private static IServiceCollection AddCommonSqlServices(this IServiceCollection services, Action<SqlServerDataStoreConfiguration> configureAction)
    {
        // Add core SQL services
        services.AddSqlServerConnection(configureAction);

        // Optionally enable workload identity
        DicomSqlServerOptions options = new();
        configureAction(options);
        if (options.EnableWorkloadIdentity)
            services.EnableWorkloadManagedIdentity();

        // Add SQL-specific data store implementations
        services
            .AddSqlExtendedQueryTagStores()
            .AddSqlExtendedQueryTagErrorStores()
            .AddSqlIndexDataStores()
            .AddSqlInstanceStores()
            .AddSqlPartitionStores()
            .AddSqlChangeFeedStores();

        return services;
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
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        return services;
    }

    private static IServiceCollection AddSqlChangeFeedStores(this IServiceCollection services)
    {
        services.TryAddScoped<IChangeFeedStore, SqlChangeFeedStore>();
        services.TryAddScoped<VersionedCache<ISqlChangeFeedStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlChangeFeedStore, SqlChangeFeedStoreV4>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlChangeFeedStore, SqlChangeFeedStoreV39>());

        return services;
    }

    private static IServiceCollection AddSqlExtendedQueryTagStores(this IServiceCollection services)
    {
        services.TryAddScoped<IExtendedQueryTagStore, SqlExtendedQueryTagStore>();
        services.TryAddScoped<VersionedCache<ISqlExtendedQueryTagStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV1>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV2>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagStore, SqlExtendedQueryTagStoreV36>());

        return services;
    }

    private static IServiceCollection AddSqlExtendedQueryTagErrorStores(this IServiceCollection services)
    {
        services.TryAddScoped<IExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStore>();
        services.TryAddScoped<VersionedCache<ISqlExtendedQueryTagErrorStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStoreV1>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStoreV4>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlExtendedQueryTagErrorStore, SqlExtendedQueryTagErrorStoreV36>());

        return services;
    }

    private static IServiceCollection AddSqlIndexDataStores(this IServiceCollection services)
    {
        services.TryAddScoped<IIndexDataStore, SqlIndexDataStore>();
        services.TryAddScoped<VersionedCache<ISqlIndexDataStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV1>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV49>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV50>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV52>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV54>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV55>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlIndexDataStore, SqlIndexDataStoreV57>());
        return services;
    }

    private static IServiceCollection AddSqlInstanceStores(this IServiceCollection services)
    {
        services.TryAddScoped<IInstanceStore, SqlInstanceStore>();
        services.TryAddScoped<VersionedCache<ISqlInstanceStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV1>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV48>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV55>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlInstanceStore, SqlInstanceStoreV58>());

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
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlQueryStore, SqlQueryStoreV27>());

        return services;
    }

    private static IServiceCollection AddSqlWorkitemStores(this IServiceCollection services)
    {
        services.TryAddScoped<IIndexWorkitemStore, SqlWorkitemStore>();
        services.TryAddScoped<VersionedCache<ISqlWorkitemStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlWorkitemStore, SqlWorkitemStoreV9>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISqlWorkitemStore, SqlWorkitemStoreV22>());

        return services;
    }

    private sealed class DicomSqlServerOptions : SqlServerDataStoreConfiguration
    {
        public bool EnableWorkloadIdentity { get; set; }
    }
}
