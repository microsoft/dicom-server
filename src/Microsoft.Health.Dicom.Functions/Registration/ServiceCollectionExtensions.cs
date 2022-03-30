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
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Configuration;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Registration;
using Microsoft.Health.Dicom.Functions.Serialization;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Operations.Functions.Management;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection;

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

        services.RegisterModule<ServiceModule>(new FeatureConfiguration { EnableExtendedQueryTags = true });

        return new DicomFunctionsBuilder(services
            .AddRecyclableMemoryStreamManager()
            .AddFellowOakDicomExtension()
            .AddFunctionsOptions<QueryTagIndexingOptions>(configuration, QueryTagIndexingOptions.SectionName, bindNonPublicProperties: true)
            .AddFunctionsOptions<PurgeHistoryOptions>(configuration, PurgeHistoryOptions.SectionName, isDicomFunction: false)
            .ConfigureDurableFunctionSerialization()
            .AddJsonSerializerOptions(o => o.ConfigureDefaultDicomSettings()));
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

    /// <summary>
    /// Adds Azure Storage implementations for storing DICOM metadata.
    /// </summary>
    /// <param name="builder">The <see cref="IDicomFunctionsBuilder"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> root.</param>
    /// <returns>The <paramref name="builder"/> for additional methods calls.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IDicomFunctionsBuilder AddMetadataStorageDataStore(this IDicomFunctionsBuilder builder, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        string containerName = configuration
            .GetSection(BlobDataStoreConfiguration.SectionName)
            .GetSection(DicomBlobContainerConfiguration.SectionName)
            .Get<DicomBlobContainerConfiguration>()
            .Metadata;

        return builder.AddMetadataStorageDataStore(configuration, containerName);
    }

    private static IServiceCollection AddRecyclableMemoryStreamManager(this IServiceCollection services, Func<RecyclableMemoryStreamManager> factory = null)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        // The custom service provider used by Azure Functions cannot seem to resolve the
        // RecyclableMemoryStreamManager ctor overloads without help, so we instantiate it ourselves
        factory ??= () => new RecyclableMemoryStreamManager();
        services.TryAddSingleton(factory());

        return services;
    }

    private static IServiceCollection AddFellowOakDicomExtension(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        // Note: Fellow Oak Services have already been added as part of the ServiceModule
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExtensionConfigProvider, FellowOakExtensionConfiguration>());

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

        return services.Replace(ServiceDescriptor.Singleton<IMessageSerializerSettingsFactory, DicomDurableTaskSerializerSettingsFactory>());
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
