// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.DicomCast.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static TConfiguration Configure<TConfiguration>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName)
            where TConfiguration : class, new()
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNullOrWhiteSpace(sectionName, nameof(sectionName));

            var config = new TConfiguration();

            configuration.GetSection(sectionName).Bind(config);

            services.AddSingleton(Options.Create(config));

            return config;
        }
    }
}
