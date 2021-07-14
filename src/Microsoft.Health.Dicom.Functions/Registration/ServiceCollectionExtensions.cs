// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Functions.Configuration;
using Microsoft.Health.SqlServer.Configs;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Dicom.Functions.Registration
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFunctionsOptions<T>(this IServiceCollection services, string sectionName)
            where T : class
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services
                .AddOptions<T>()
                .Configure<IConfiguration>((sectionObj, config) => config
                    .GetSection(AzureFunctionsJobHost.ConfigurationSectionName)
                    .GetSection(DicomFunctionsConfiguration.SectionName)
                    .GetSection(sectionName)
                    .Bind(sectionObj));

            return services;
        }

        public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            new DicomFunctionsBuilder(services)
                .AddSqlServer(c => configuration.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(c))
                .AddMetadataStorageDataStore(c => configuration.GetSection(BlobDataStoreConfiguration.SectionName).Bind(c));

            return services;
        }

        public static IServiceCollection AddHttpServices(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services
                .AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                    .Add(new StringEnumConverter()));

            return services;
        }
    }
}
