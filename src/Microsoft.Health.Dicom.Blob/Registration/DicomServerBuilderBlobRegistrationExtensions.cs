// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Metadata.Utilities;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderBlobRegistrationExtensions
    {
        /// <summary>
        /// Adds the blob data store for the DICOM server.
        /// </summary>
        /// <param name="serverBuilder">The DICOM server builder instance.</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>The server builder.</returns>
        public static IDicomServerBuilder AddDataStore<TStoreConfigurationAware>(
            this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);

            serverBuilder.Services
                .AddBlobServiceClient(blobConfig)
                .AddPersistence()
                .AddHealthCheck()
                .AddOptions<BlobOperationOptions>()
                .Bind(blobConfig.GetSection(nameof(BlobServiceClientOptions.Operations)));

            serverBuilder
                .AddStorage<BlobContainerConfigurationAware>(configuration)
                .AddStorage<MetadataContainerConfigurationAware>(configuration);

            return serverBuilder;
        }

        private static IDicomServerBuilder AddStorage<TStoreConfigurationAware>(
            this IDicomServerBuilder serverBuilder, IConfiguration configuration)
            where TStoreConfigurationAware : IStoreConfigurationAware, new()
        {
            var config = new TStoreConfigurationAware();
            var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);

            serverBuilder.Services
                .AddBlobContainerInitialization(x => blobConfig
                    .GetSection(BlobInitializerOptions.DefaultSectionName)
                    .Bind(x))
                .ConfigureContainer(config.Name, x => configuration
                    .GetSection(config.SectionName)
                    .Bind(x));

            return serverBuilder;
        }

        /// <summary>
        /// Adds the metadata store for the DICOM functions.
        /// </summary>
        /// <param name="functionsBuilder">The DICOM functions builder instance.</param>
        /// <param name="containerName">The name of the metadata container.</param>
        /// <param name="configuration">The configuration for the function.</param>
        /// <returns>The functions builder.</returns>
        public static IDicomFunctionsBuilder AddMetadataStorageDataStore(
            this IDicomFunctionsBuilder functionsBuilder,
            IConfiguration configuration,
            string containerName)
        {
            EnsureArg.IsNotNull(functionsBuilder, nameof(functionsBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            var blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
            functionsBuilder.Services
                .AddBlobServiceClient(blobConfig)
                .AddPersistence<IMetadataStore, BlobMetadataStore, LoggingMetadataStore>()
                .Configure<BlobContainerConfiguration>(MetadataContainerConfigurationAware.ConfigurationSectionName, c => c.ContainerName = containerName);

            return functionsBuilder;
        }

        private static IServiceCollection AddPersistence(this IServiceCollection services)
        {
            services
                .AddPersistence<IFileStore, BlobFileStore, LoggingFileStore>()
                .AddPersistence<IMetadataStore, BlobMetadataStore, LoggingMetadataStore>();

            return services;
        }

        private static IServiceCollection AddHealthCheck(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services
                .AddHealthChecks()
                .AddCheck<DicomBlobHealthCheck<BlobContainerConfigurationAware>>(name: "DcmHealthCheck")
                .AddCheck<DicomBlobHealthCheck<MetadataContainerConfigurationAware>>(name: "MetadataHealthCheck");

            return services;
        }

        private static IServiceCollection AddPersistence<TIStore, TBlobStore, TLogger>(this IServiceCollection services)
            where TBlobStore : TIStore
            where TLogger : TIStore
        {
            services.Add<TBlobStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<TIStore, TLogger>();

            return services;
        }
    }
}
