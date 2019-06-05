// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.CosmosDb.Features.Storage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DicomServerBuilderCosmosDbRegistrationExtensions
    {
        /// <summary>
        /// Adds Cosmos as the data store for the DICOM server.
        /// </summary>
        /// <param name="serverBuilder">The DICOM server builder instance.</param>
        /// <param name="configuration">The configuration for the server.</param>
        /// <returns>The server builder.</returns>
        public static IDicomServerBuilder AddCosmosDbDataStore(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            return serverBuilder.AddCosmosDbPersistence();
        }

        private static IDicomServerBuilder AddCosmosDbPersistence(this IDicomServerBuilder serverBuilder)
        {
            IServiceCollection services = serverBuilder.Services;

            services.Add<DicomDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return serverBuilder;
        }
    }
}
