// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Dicom.Core.Extensions
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Register type as Singleton with all implemented inferfaces and itself.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddSingletonDefault<T>(this IServiceCollection services)
        {
            services.Add<T>()
                  .Singleton()
                  .AsSelf()
                  .AsImplementedInterfaces();
            return services;
        }

        /// <summary>
        /// Register type as Scoped with all implemented inferfaces and itself.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddScopedDefault<T>(this IServiceCollection services)
        {
            services.Add<T>()
                  .Scoped()
                  .AsSelf()
                  .AsImplementedInterfaces();
            return services;
        }

        /// <summary>
        /// Register type as Transient with all implemented inferfaces and itself.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddTransientDefault<T>(this IServiceCollection services)
        {
            services.Add<T>()
                  .Transient()
                  .AsSelf()
                  .AsImplementedInterfaces();
            return services;
        }
    }
}
