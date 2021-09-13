// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Metadata;
using Microsoft.Health.Dicom.Metadata.Features.Health;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderMetadataRegistrationExtensions
    {
        private const string DicomServerBlobConfigurationSectionName = "DicomWeb:MetadataStore";

        /// <summary>
        /// Adds the metadata store for the DICOM server.
        /// </summary>
        /// <param name="serverBuilder">The DICOM server builder instance.</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>The server builder.</returns>
        public static IDicomServerBuilder AddMetadataStorageDataStore(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            IConfiguration blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
            serverBuilder.Services
                .AddMetadataPersistence()
                .AddBlobServiceClient(blobConfig)
                .AddBlobContainerInitialization(x => blobConfig
                    .GetSection(BlobInitializerOptions.DefaultSectionName)
                    .Bind(x))
                .ConfigureContainer(Constants.ContainerConfigurationName, x => configuration
                    .GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(x));

            return serverBuilder.AddMetadataHealthCheck();
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

            IConfiguration blobConfig = configuration.GetSection(BlobServiceClientOptions.DefaultSectionName);
            functionsBuilder.Services
                .AddMetadataPersistence()
                .AddBlobServiceClient(blobConfig)
                .Configure<BlobContainerConfiguration>(Constants.ContainerConfigurationName, c => c.ContainerName = containerName);

            return functionsBuilder;
        }

        private static IServiceCollection AddMetadataPersistence(this IServiceCollection services)
        {
            services.Add<BlobMetadataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            // TODO: Ideally, the logger can be registered in the API layer since it's agnostic to the implementation.
            // However, the current implementation of the decorate method requires the concrete type to be already registered,
            // so we need to register here. Need to some more investigation to see how we might be able to do this.
            services.Decorate<IMetadataStore, LoggingMetadataStore>();

            return services;
        }

        private static IDicomServerBuilder AddMetadataHealthCheck(this IDicomServerBuilder serverBuilder)
        {
            serverBuilder.Services.AddHealthChecks().AddCheck<MetadataHealthCheck>(name: "MetadataHealthCheck");
            return serverBuilder;
        }
    }
}
