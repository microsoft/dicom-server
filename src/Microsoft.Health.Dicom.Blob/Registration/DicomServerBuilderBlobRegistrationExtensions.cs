// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Features.Telemetry;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Blob.Features.ExternalStore;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class DicomServerBuilderBlobRegistrationExtensions
{
    /// <summary>
    /// Adds the blob data stores for the DICOM server.
    /// </summary>
    /// <param name="serverBuilder">The DICOM server builder instance.</param>
    /// <param name="configuration">The configuration for the server.</param>
    /// <returns>The server builder.</returns>
    public static IDicomServerBuilder AddBlobDataStores(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        IConfigurationSection blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
        serverBuilder.Services
            .AddOptions<BlobOperationOptions>()
            .Bind(blobConfig.GetSection(nameof(BlobServiceClientOptions.Operations)));

        FeatureConfiguration featureConfiguration = new FeatureConfiguration();
        configuration.GetSection("DicomServer").GetSection("Features").Bind(featureConfiguration);
        if (featureConfiguration.EnableExternalStore)
        {
            serverBuilder.Services
                .AddOptions<ExternalBlobDataStoreConfiguration>()
                .Bind(configuration.GetSection(ExternalBlobDataStoreConfiguration.SectionName))
                .ValidateDataAnnotations();

            serverBuilder.Services.Add<ExternalBlobClient>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            serverBuilder.Services
                .AddPersistence<IFileStore, BlobFileStore>();

            serverBuilder.Services
                .AddHealthChecks()
                .AddCheck<DicomConnectedStoreHealthCheck>("DcmHealthCheck");
        }
        else
        {
            serverBuilder.Services.Add<InternalBlobClient>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            serverBuilder
                .AddStorageDataStore<BlobStoreConfigurationSection, IFileStore, BlobFileStore>(
                    configuration,
                    "DcmHealthCheck");
        }

        serverBuilder
            .AddStorageDataStore<MetadataStoreConfigurationSection, IMetadataStore, BlobMetadataStore>(
                configuration,
                "MetadataHealthCheck")
            .AddStorageDataStore<WorkitemStoreConfigurationSection, IWorkitemStore, BlobWorkitemStore>(
                configuration,
                "WorkitemHealthCheck");

        serverBuilder.Services
            .AddAzureBlobExportSink(
                o => configuration.GetSection(AzureBlobExportSinkProviderOptions.DefaultSection).Bind(o),
                o => blobConfig.Bind(o)); // Re-use the blob store's configuration for the client

        serverBuilder.Services
            .AddSingleton<BlobStoreMeter>()
            .AddSingleton<BlobRetrieveMeter>()
            .AddSingleton<BlobFileStoreMeter>();


        return serverBuilder;
    }

    private static IDicomServerBuilder AddStorageDataStore<TStoreConfigurationSection, TIStore, TStore>(
        this IDicomServerBuilder serverBuilder, IConfiguration configuration, string healthCheckName)
        where TStoreConfigurationSection : class, IStoreConfigurationSection, new()
        where TStore : class, TIStore
    {
        var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);

        var config = new TStoreConfigurationSection();

        serverBuilder.Services
            .AddSingleton<TStoreConfigurationSection>()
            .AddTransient<IStoreConfigurationSection>(sp => sp.GetRequiredService<TStoreConfigurationSection>())
            .AddPersistence<TIStore, TStore>()
            .AddBlobServiceClient(blobConfig)
            .AddScoped<DicomFileNameWithPrefix>()
            .AddBlobContainerInitialization(x => blobConfig
                .GetSection(BlobInitializerOptions.DefaultSectionName)
                .Bind(x))
            .ConfigureContainer(config.ContainerConfigurationName, x => configuration
                .GetSection(config.ConfigurationSectionName)
                .Bind(x));

        serverBuilder
            .AddBlobHealthCheck<DicomBlobHealthCheck<TStoreConfigurationSection>>(healthCheckName);

        return serverBuilder;
    }

    internal static IServiceCollection AddPersistence<TIStore, TStore>(this IServiceCollection services)
        where TStore : class, TIStore
    {
        services.Add<TStore>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        return services;
    }

    internal static IDicomServerBuilder AddBlobHealthCheck<TBlobHealthCheck>(this IDicomServerBuilder serverBuilder, string name)
        where TBlobHealthCheck : BlobHealthCheck
    {
        serverBuilder.Services
            .AddHealthChecks()
            .AddCheck<TBlobHealthCheck>(name: name);

        return serverBuilder;
    }
}
