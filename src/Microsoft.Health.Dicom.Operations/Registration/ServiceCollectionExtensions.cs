// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Operations.Configuration;
using Microsoft.Health.Dicom.Operations.Durable;
using Microsoft.Health.Dicom.Operations.Indexing;
using Microsoft.Health.Dicom.Operations.Management;
using Microsoft.Health.Dicom.Operations.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IDicomFunctionsBuilder ConfigureFunctions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            services.RegisterModule<ServiceModule>(new FeatureConfiguration { EnableExtendedQueryTags = true });

            return new DicomFunctionsBuilder(services
                .AddRecyclableMemoryStreamManager()
                .AddDicomJsonNetSerialization()
                .AddFunctionsOptions<QueryTagIndexingOptions>(configuration, QueryTagIndexingOptions.SectionName, bindNonPublicProperties: true)
                .AddFunctionsOptions<PurgeHistoryOptions>(configuration, PurgeHistoryOptions.SectionName)
                .AddDurableFunctionServices()
                .AddHttpServices());
        }

        public static IDicomFunctionsBuilder AddSqlServer(this IDicomFunctionsBuilder builder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            return builder.AddSqlServer(c => configuration.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(c));
        }

        public static IDicomFunctionsBuilder AddMetadataStorageDataStore(this IDicomFunctionsBuilder builder, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            string containerName = configuration
                .GetSection(BlobDataStoreConfiguration.SectionName)
                .GetSection(DicomBlobContainerConfiguration.SectionName)
                .Get<DicomBlobContainerConfiguration>()
                .Metadata;

            return builder.AddMetadataStorageDataStore(configuration, containerName);
        }

        private static IServiceCollection AddRecyclableMemoryStreamManager(this IServiceCollection services, Func<RecyclableMemoryStreamManager> factory = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            // The custom service provider used by Azure Functions cannot seem to resolve the
            // RecyclableMemoryStreamManager ctor overloads without help, so we instantiate it ourselves
            factory ??= () => new RecyclableMemoryStreamManager();
            services.TryAddSingleton(factory());

            return services;
        }

        private static IServiceCollection AddFunctionsOptions<T>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName,
            bool bindNonPublicProperties = false)
            where T : class
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotEmptyOrWhiteSpace(sectionName, nameof(sectionName));

            services
                .AddOptions<T>()
                .Bind(
                    configuration
                        .GetSection(DicomFunctionsConfiguration.SectionName)
                        .GetSection(sectionName),
                    x => x.BindNonPublicProperties = bindNonPublicProperties)
                .ValidateDataAnnotations();

            return services;
        }

        private static IServiceCollection AddDurableFunctionServices(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.TryAddSingleton(GuidFactory.Default);

            return services;
        }

        public static IServiceCollection AddHttpServices(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services
                .AddMvcCore()
                .AddJsonSerializerOptions(x => x.Converters.Add(new JsonStringEnumConverter()));

            return services;
        }

        private static IMvcCoreBuilder AddJsonSerializerOptions(this IMvcCoreBuilder builder, Action<JsonSerializerOptions> configure)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            EnsureArg.IsNotNull(configure, nameof(configure));

            // TODO: Configure System.Text.Json for Azure Functions when available
            //builder.AddJsonOptions(o => configure(o.JsonSerializerOptions));
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
