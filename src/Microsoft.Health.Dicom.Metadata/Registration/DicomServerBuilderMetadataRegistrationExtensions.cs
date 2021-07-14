// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
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

            serverBuilder.Services
                .AddBlobDataStore(configuration)
                .AddMetadataStore()
                .AddMetadataHealthCheck();

            return serverBuilder;
        }

        /// <summary>
        /// Adds the metadata store for the DICOM functions.
        /// </summary>
        /// <param name="functionsBuilder">The DICOM functions builder instance.</param>
        /// <param name="configure">A delegate for configuring the underlying blob storage client.</param>
        /// <returns>The functions builder.</returns>
        public static IDicomFunctionsBuilder AddMetadataStorageDataStore(this IDicomFunctionsBuilder functionsBuilder, Action<BlobDataStoreConfiguration> configure)
        {
            EnsureArg.IsNotNull(functionsBuilder, nameof(functionsBuilder));
            EnsureArg.IsNotNull(configure, nameof(configure));

            functionsBuilder.Services
                .AddBlobServiceClient(configure)
                .AddMetadataStore();

            return functionsBuilder;
        }

        private static IServiceCollection AddBlobDataStore(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddBlobDataStore();

            services.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configuration.GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

            services.Add(
                sp =>
                {
                    ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();
                    IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfiguration = sp.GetService<IOptionsMonitor<BlobContainerConfiguration>>();
                    BlobContainerConfiguration blobContainerConfiguration = namedBlobContainerConfiguration.Get(Constants.ContainerConfigurationName);

                    return new BlobContainerInitializer(
                        blobContainerConfiguration.ContainerName,
                        loggerFactory.CreateLogger<BlobContainerInitializer>());
                })
                .Singleton()
                .AsService<IBlobContainerInitializer>();

            return services;
        }

        private static IServiceCollection AddMetadataStore(this IServiceCollection services)
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

        private static IServiceCollection AddMetadataHealthCheck(this IServiceCollection services)
        {
            services
                .AddHealthChecks()
                .AddCheck<MetadataHealthCheck>(name: "MetadataHealthCheck");

            return services;
        }
    }
}
