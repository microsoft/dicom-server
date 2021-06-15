// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Operations.Functions.Registration;
using Microsoft.Health.Dicom.Operations.Functions.Indexing.Configuration;
using Newtonsoft.Json.Converters;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Functions.Startup))]
namespace Microsoft.Health.Dicom.Functions
{
    public class Startup : FunctionsStartup
    {
        private const string AzureFunctionsJobHostSection = "AzureFunctionsJobHost";
        public override void Configure(IFunctionsHostBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            builder.Services
                .AddOptions<IndexingConfiguration>()
                .Configure<IConfiguration>((sectionObj, config) => config
                    .GetSection(HostSectionName)
                    .GetSection(IndexingConfiguration.SectionName)
                    .Bind(sectionObj));

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                    .Add(new StringEnumConverter()));
            IConfiguration configuration = builder.GetContext().Configuration?.GetSection(AzureFunctionsJobHostSection);
            builder.Services.AddDicomOperations(configuration)
                .AddDicomOperationsCore()
                .AddSqlServer(configuration);
        }
    }
}
