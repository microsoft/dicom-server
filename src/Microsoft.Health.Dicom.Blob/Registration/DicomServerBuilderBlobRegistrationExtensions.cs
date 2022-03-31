// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Export;
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

        var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
        serverBuilder.Services
            .AddOptions<BlobOperationOptions>()
            .Bind(blobConfig.GetSection(nameof(BlobServiceClientOptions.Operations)));

        serverBuilder
            .AddStorageDataStore<BlobStoreConfigurationSection, IFileStore, BlobFileStore, LoggingFileStore>(
                configuration, "DcmHealthCheck")
            .AddStorageDataStore<MetadataStoreConfigurationSection, IMetadataStore, BlobMetadataStore, LoggingMetadataStore>(
                configuration, "MetadataHealthCheck")
            .AddStorageDataStore<WorkitemStoreConfigurationSection, IWorkitemStore, BlobWorkitemStore, LoggingWorkitemStore>(
                configuration, "WorkitemHealthCheck");

        serverBuilder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IExportSinkProvider, AzureBlobExportSinkProvider>());

        return serverBuilder;
    }

    private static IDicomServerBuilder AddStorageDataStore<TStoreConfigurationSection, TIStore, TStore, TLogStore>(
        this IDicomServerBuilder serverBuilder, IConfiguration configuration, string healthCheckName)
        where TStoreConfigurationSection : class, IStoreConfigurationSection, new()
        where TStore : class, TIStore
        where TLogStore : TIStore
    {
        var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);

        var config = new TStoreConfigurationSection();

        serverBuilder.Services
            .AddSingleton<TStoreConfigurationSection>()
            .AddTransient<IStoreConfigurationSection>(sp => sp.GetRequiredService<TStoreConfigurationSection>())
            .AddPersistence<TIStore, TStore, TLogStore>()
            .AddBlobServiceClient(blobConfig)
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

    internal static IServiceCollection AddPersistence<TIStore, TStore, TLogStore>(this IServiceCollection services)
        where TStore : class, TIStore
        where TLogStore : TIStore
    {
        services.Add<TStore>()
            .Scoped()
            .AsSelf()
            .AsImplementedInterfaces();

        // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
        // However, the current implementation of the decorate method requires the concrete type to be already registered,
        // so we need to register here. Need to some more investigation to see how we might be able to do this.
        services.Decorate<TIStore, TLogStore>();

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
