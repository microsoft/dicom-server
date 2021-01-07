// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
{
    public static class ExceptionStoreExtension
    {
        /// <summary>
        /// Adds default exception store.
        /// </summary>
        /// <param name="serviceCollection">Service collection</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddDefaultExceptionStore(this IServiceCollection serviceCollection)
        {
            EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));

            serviceCollection.Add<LogExceptionStore>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            return serviceCollection;
        }
    }
}
