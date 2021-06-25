// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Operations.Functions.Registration;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Functions.Startup))]
namespace Microsoft.Health.Dicom.Functions
{
    public class Startup : FunctionsStartup
    {
        private const string AzureFunctionsJobHostSection = "AzureFunctionsJobHost";
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            IConfiguration config = builder.GetContext().Configuration.GetSection(AzureFunctionsJobHost.SectionName);

            builder.Services
                .AddOptions<IndexingConfiguration>()
                .Configure<IConfiguration>((sectionObj, config) => config
                    .GetSection(AzureFunctionsJobHost.SectionName)
                    .GetSection(IndexingConfiguration.SectionName)
                    .Bind(sectionObj));

            builder.Services
                .AddSqlServer(config)
                .AddForegroundSchemaVersionResolution()
                .AddExtendedQueryTagStores();

            builder.Services
                .AddAzureBlobServiceClient(config)
                .AddMetadataStore();

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                    .Add(new StringEnumConverter()));
            EnsureArg.IsNotNull(builder, nameof(builder));
            IConfiguration configuration = builder.GetContext().Configuration?.GetSection(AzureFunctionsJobHostSection);
            builder.Services.AddDicomFunctions(configuration)
                .AddSqlForAzureFunction(configuration);
        }
    }
}
