// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BlobClientRegistrationExtensions
    {
        public const string BlobStoreConfigurationSectionName = "BlobStore";

        public static IServiceCollection AddBlobDataStore(this IServiceCollection services, Action<BlobDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            if (services.Any(x => x.ImplementationType == typeof(BlobClientProvider)))
            {
                return services;
            }

            services.Add(provider =>
                {
                    var config = new BlobDataStoreConfiguration();
                    provider.GetService<IConfiguration>().GetSection(BlobStoreConfigurationSectionName).Bind(config);
                    configureAction?.Invoke(config);

                    if (string.IsNullOrEmpty(config.ConnectionString))
                    {
                        config.ConnectionString = BlobLocalEmulator.ConnectionString;
                    }

                    return config;
                })
                .Singleton()
                .AsSelf();

            services.Add<BlobClientProvider>()
                .Singleton()
                .AsSelf()
                .AsService<IStartable>() // so that it starts initializing ASAP
                .AsService<IRequireInitializationOnFirstRequest>(); // so that web requests block on its initialization.

            services.Add(sp => sp.GetService<BlobClientProvider>().CreateBlobClient())
                .Singleton()
                .AsSelf();

            services.Add<BlobClientReadWriteTestProvider>()
                .Singleton()
                .AsService<IBlobClientTestProvider>();

            services.Add<BlobClientInitializer>()
                .Singleton()
                .AsService<IBlobClientInitializer>();

            return services;
        }
    }
}
