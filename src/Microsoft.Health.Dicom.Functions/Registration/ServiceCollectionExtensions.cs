// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Configuration;
using Microsoft.Health.Dicom.Functions.ContentLengthBackFill;
using Microsoft.Health.Dicom.Functions.DataCleanup;
using Microsoft.Health.Dicom.Functions.Export;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.MetricsCollection;
using Microsoft.Health.Dicom.Functions.Update;
using Microsoft.Health.Dicom.SqlServer.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Operations.Functions.DurableTask;
using Microsoft.Health.Operations.Functions.Management;
using Microsoft.Health.SqlServer.Configs;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Registration;

/// <summary>
/// A <see langword="static"/> collection of methods for configuring DICOM Azure Functions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core set of services required to run the DICOM operations as Azure Functions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> root.</param>
    /// <returns>A corresponding <see cref="IDicomFunctionsBuilder"/> to add additional services.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IDicomFunctionsBuilder ConfigureFunctions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        services.RegisterModule<ServiceModule>();

        return new DicomFunctionsBuilder(services
            .AddRecyclableMemoryStreamManager(configuration)
            .AddFellowOakDicomExtension()
            .AddFunctionsOptions<DataCleanupOptions>(configuration, DataCleanupOptions.SectionName)
            .AddFunctionsOptions<ContentLengthBackFillOptions>(configuration, ContentLengthBackFillOptions.SectionName)
            .AddFunctionsOptions<ExportOptions>(configuration, ExportOptions.SectionName)
            .AddFunctionsOptions<QueryTagIndexingOptions>(configuration, QueryTagIndexingOptions.SectionName, bindNonPublicProperties: true)
            .AddFunctionsOptions<PurgeHistoryOptions>(configuration, PurgeHistoryOptions.SectionName, isDicomFunction: false)
            .AddFunctionsOptions<FeatureConfiguration>(configuration, "DicomServer:Features", isDicomFunction: false)
            .AddFunctionsOptions<UpdateOptions>(configuration, UpdateOptions.SectionName)
            .AddFunctionsOptions<IndexMetricsCollectionOptions>(configuration, IndexMetricsCollectionOptions.SectionName)
            .ConfigureDurableFunctionSerialization()
            .AddJsonSerializerOptions(o => o.ConfigureDefaultDicomSettings())
            .AddSingleton<UpdateMeter>()
            .AddSingleton<IAuditLogger, AuditLogger>());
    }

    /// <summary>
    /// Adds the metadata store for the DICOM functions.
    /// </summary>
    /// <param name="builder">The DICOM functions builder instance.</param>
    /// <param name="configuration">The host configuration for the functions.</param>
    /// <returns>The functions builder.</returns>
    public static IDicomFunctionsBuilder AddBlobStorage(this IDicomFunctionsBuilder builder, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        return builder.AddBlobStorage(configuration, DicomFunctionsConfiguration.SectionName);
    }

    /// <summary>
    /// Adds MSSQL Server implementations for indexing DICOM data and storing its metadata.
    /// </summary>
    /// <param name="builder">The <see cref="IDicomFunctionsBuilder"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> root.</param>
    /// <returns>The <paramref name="builder"/> for additional methods calls.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IDicomFunctionsBuilder AddSqlServer(this IDicomFunctionsBuilder builder, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        return builder.AddSqlServer(c => configuration.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(c));
    }

    private static IServiceCollection AddFellowOakDicomExtension(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        // Note: Fellow Oak Services have already been added as part of the ServiceModule
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExtensionConfigProvider, FellowOakExtensionConfiguration>());

        CustomDicomImplementation.SetDicomImplementationClassUIDAndVersion();

        return services;
    }

    private static IServiceCollection AddFunctionsOptions<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        bool isDicomFunction = true,
        bool bindNonPublicProperties = false)
        where T : class
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configuration, nameof(configuration));
        EnsureArg.IsNotEmptyOrWhiteSpace(sectionName, nameof(sectionName));

        string path = isDicomFunction ? DicomFunctionsConfiguration.SectionName + ":" + sectionName : sectionName;
        services
            .AddOptions<T>()
            .Bind(
                configuration.GetSection(path),
                x => x.BindNonPublicProperties = bindNonPublicProperties)
            .ValidateDataAnnotations();

        return services;
    }

    private static IServiceCollection AddJsonSerializerOptions(this IServiceCollection services, Action<JsonSerializerOptions> configure)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configure, nameof(configure));

        // TODO: Configure System.Text.Json for Azure Functions MVC services when available
        //       and if we decide to expose HTTP services
        //builder.AddJsonOptions(o => configure(o.JsonSerializerOptions));
        return services.Configure(configure);
    }

    private static IServiceCollection ConfigureDurableFunctionSerialization(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.Configure<JsonSerializerSettings>(o => o.ConfigureDefaultDicomSettings());
        return services.Replace(ServiceDescriptor.Singleton<IMessageSerializerSettingsFactory, MessageSerializerSettingsFactory>());
    }

    private sealed class FellowOakExtensionConfiguration : IExtensionConfigProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public FellowOakExtensionConfiguration(IServiceProvider serviceProvider)
            => _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

        public void Initialize(ExtensionConfigContext context)
            => DicomSetupBuilder.UseServiceProvider(_serviceProvider);
    }
}
