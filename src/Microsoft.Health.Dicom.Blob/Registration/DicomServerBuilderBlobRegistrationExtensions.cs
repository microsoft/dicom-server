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
using Microsoft.Health.Dicom.Blob;
using Microsoft.Health.Dicom.Blob.Features.Health;
using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderBlobRegistrationExtensions
    {
        private static readonly string DicomServerBlobConfigurationSectionName = $"DicomWeb:{BlobClientRegistrationExtensions.BlobStoreConfigurationSectionName}";

        /// <summary>
        /// Add blob as the data store for the DICOM server.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>The collection of services.</returns>
        public static IServiceCollection AddDicomServerBlob(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            return serviceCollection
                        .AddBlobPersistence(configuration)
                        .AddBlobHealthCheck();
        }

        private static IServiceCollection AddBlobPersistence(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddBlobDataStore();

            serviceCollection.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configuration.GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

            serviceCollection.Add<DicomBlobDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            serviceCollection.Add(sp =>
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

            return serviceCollection;
        }

        private static IServiceCollection AddBlobHealthCheck(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddHealthChecks().AddCheck<DicomBlobHealthCheck>(name: nameof(DicomBlobHealthCheck));
            return serviceCollection;
        }
    }
}
