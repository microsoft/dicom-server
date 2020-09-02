// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.DicomCast.Blob.Features.Health;
using Microsoft.Health.DicomCast.Blob.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.Blob.Registration
{
    public static class DicomCastBlobRegistrationExtension
    {
        private static readonly string DicomCastSyncStateBlobConfigurationSectionName = $"DicomCastStores:SyncStateStore";

        /// <summary>
        /// Adds the blob data store for dicom cast.
        /// </summary>
        /// <param name="serviceCollection">Service collection</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddBlobStorageDataStore(this IServiceCollection serviceCollection, IConfiguration configuration)
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
                Constants.ContainerConfigurationOptionsName,
                containerConfiguration => configuration.GetSection(DicomCastSyncStateBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

            serviceCollection.Add<SyncStateStore>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            serviceCollection.Add(sp =>
            {
                ILoggerFactory loggerFactory = sp.GetService<ILoggerFactory>();
                IOptionsMonitor<BlobContainerConfiguration> namedBlobContainerConfiguration = sp.GetService<IOptionsMonitor<BlobContainerConfiguration>>();
                BlobContainerConfiguration blobContainerConfiguration = namedBlobContainerConfiguration.Get(Constants.ContainerConfigurationOptionsName);

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
            serviceCollection.AddHealthChecks().AddCheck<DicomCastBlobHealthCheck>(name: nameof(DicomCastBlobHealthCheck));
            return serviceCollection;
        }
    }
}
