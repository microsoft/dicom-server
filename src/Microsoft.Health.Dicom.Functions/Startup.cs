// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Functions.Management;
using Microsoft.Health.Dicom.Operations.Functions.Configs;
using Newtonsoft.Json.Converters;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Functions.Startup))]
namespace Microsoft.Health.Dicom.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));
            var services = builder.Services;
            IConfiguration config = builder.GetContext().Configuration.GetSection(AzureFunctionsJobHost.SectionName);

            DicomFunctionsConfiguration dicomFuncionsConfig = new DicomFunctionsConfiguration();
            config?.GetSection(DicomFunctionsConfiguration.SectionName).Bind(dicomFuncionsConfig);
            services.AddSingleton(Options.Create(dicomFuncionsConfig));
            services.AddSingleton(Options.Create(dicomFuncionsConfig.Reindex));

            services.AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                .Add(new StringEnumConverter()));

            builder.Services
                .AddSqlServer(config)
                .AddForegroundSchemaVersionResolution()
                .AddExtendedQueryTagStores();

            builder.Services
                .AddAzureBlobServiceClient(config)
                .AddMetadataStore();

            // TODO: the FeatureConfiguration should be removed once we moved the logic to add tags into database out of Azure Function
            new ServiceModule(new FeatureConfiguration { EnableExtendedQueryTags = true }).Load(builder.Services);

            builder.Services
                .AddOptions<OrchestrationHistoryConfiguration>()
                .Configure<IConfiguration>((sectionObj, config) => config
                    .GetSection(AzureFunctionsJobHost.SectionName)
                    .GetSection(OrchestrationHistoryConfiguration.SectionName)
                    .Bind(sectionObj));

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                    .Add(new StringEnumConverter()));
        }
    }
}
