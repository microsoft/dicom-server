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
using Microsoft.Health.Dicom.Transactional;
using Microsoft.Health.Dicom.Transactional.Features.Health;
using Microsoft.Health.Dicom.Transactional.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderTransactionalRegistrationExtensions
    {
        private static readonly string DicomServerBlobConfigurationSectionName = $"DicomWeb:MetadataStore";

        /// <summary>
        /// Adds the transactional service for the DICOM server.
        /// </summary>
        /// <param name="serverBuilder">The DICOM server builder instance.</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>The server builder.</returns>
        public static IDicomServerBuilder AddTransactionalServices(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            return serverBuilder
                        .AddTransactionService(configuration)
                        .AddTransactionHealthCheck();
        }

        private static IDicomServerBuilder AddTransactionService(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            IServiceCollection services = serverBuilder.Services;

            services.AddBlobDataStore();

            services.Configure<BlobContainerConfiguration>(
                Constants.ContainerConfigurationName,
                containerConfiguration => configuration.GetSection(DicomServerBlobConfigurationSectionName)
                    .Bind(containerConfiguration));

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

            services.Add<DicomTransactionService>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DicomTransactionResolver>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return serverBuilder;
        }

        private static IDicomServerBuilder AddTransactionHealthCheck(this IDicomServerBuilder serverBuilder)
        {
            serverBuilder.Services.AddHealthChecks().AddCheck<DicomTransactionHealthCheck>(name: nameof(DicomTransactionHealthCheck));
            return serverBuilder;
        }
    }
}
