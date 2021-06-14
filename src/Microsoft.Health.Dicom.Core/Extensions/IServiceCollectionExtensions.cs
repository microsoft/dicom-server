// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

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

        public static IServiceCollection AddScopedDefault<T1, T2>(this IServiceCollection services)
        {
            services.AddScopedDefault<T1>();
            services.AddScopedDefault<T2>();
            return services;
        }

        public static IServiceCollection AddScopedDefault<T1, T2, T3>(this IServiceCollection services)
        {
            services.AddScopedDefault<T1>();
            services.AddScopedDefault<T2>();
            services.AddScopedDefault<T3>();
            return services;
        }
        public static IServiceCollection AddScopedDefault<T1, T2, T3, T4>(this IServiceCollection services)
        {
            services.AddScopedDefault<T1>();
            services.AddScopedDefault<T2>();
            services.AddScopedDefault<T3>();
            services.AddScopedDefault<T4>();
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
