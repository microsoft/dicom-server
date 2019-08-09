// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Metadata;
using Microsoft.Health.Dicom.Metadata.Config;
using Microsoft.Health.Dicom.Metadata.Features.Health;
using Microsoft.Health.Dicom.Metadata.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderMetadataRegistrationExtensions
    {
        private static readonly string DicomServerBlobConfigurationSectionName = $"DicomWeb:MetadataStore";

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

            return serverBuilder
                        .AddMetadataPersistence(configuration)
                        .AddMetadataHealthCheck();
        }

        private static IDicomServerBuilder AddMetadataPersistence(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            IServiceCollection services = serverBuilder.Services;

            services.AddBlobDataStore();

            services.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configuration.GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

            // Add the metadata configuration; this is not loaded from the settings configuration for now.
            services.Add<DicomMetadataConfiguration>()
                .Singleton()
                .AsSelf();

            services.Add(sp =>
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

            services.Add<DicomMetadataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomInstanceMetadataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return serverBuilder;
        }

        private static IDicomServerBuilder AddMetadataHealthCheck(this IDicomServerBuilder serverBuilder)
        {
            serverBuilder.Services.AddHealthChecks().AddCheck<DicomMetadataHealthCheck>(name: nameof(DicomMetadataHealthCheck));
            return serverBuilder;
        }
    }
}
