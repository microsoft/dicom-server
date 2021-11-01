// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Functions.Configuration;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Management;
using Microsoft.Health.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Functions.Startup))]
namespace Microsoft.Health.Dicom.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            IConfiguration config = builder
                .GetContext()
                .Configuration
                .GetSection(DicomFunctionsConfiguration.HostSectionName);

            // Common DICOM Services
            builder.Services
                .AddRecyclableMemoryStreamManager()
                .AddFellowOakDicomServices(skipValidation: true)
                .AddStorageServices(config);

            // Function Services
            builder.Services
                .AddFunctionsOptions<QueryTagIndexingOptions>(config, QueryTagIndexingOptions.SectionName, bindNonPublicProperties: true)
                .AddFunctionsOptions<PurgeHistoryOptions>(config, PurgeHistoryOptions.SectionName)
                .AddDurableFunctionServices()
                .AddHttpServices();

            builder.Services.RegisterModule<ServiceModule>(new FeatureConfiguration { EnableExtendedQueryTags = true });

            // Add Extension for Fellow Oak
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IExtensionConfigProvider, FellowOakConfiguration>());
        }

        private sealed class FellowOakConfiguration : IExtensionConfigProvider
        {
            private readonly IServiceProvider _serviceProvider;

            public FellowOakConfiguration(IServiceProvider serviceProvider)
                => _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

            public void Initialize(ExtensionConfigContext context)
                => DicomSetupBuilder.UseServiceProvider(_serviceProvider);
        }
    }
}
