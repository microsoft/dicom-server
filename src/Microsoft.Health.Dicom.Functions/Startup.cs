// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Modules;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Configuration;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Management;
using Microsoft.Health.Dicom.Functions.Registration;

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

            builder.Services
                .AddFunctionsOptions<QueryTagIndexingOptions>(config, QueryTagIndexingOptions.SectionName)
                .AddFunctionsOptions<PurgeHistoryOptions>(config, PurgeHistoryOptions.SectionName)
                .AddDicomJsonNetSerialization()
                .AddStorageServices(config)
                .AddHttpServices();

            // TODO: the FeatureConfiguration should be removed once we moved the logic to add tags into database out of Azure Function
            new ServiceModule(new FeatureConfiguration { EnableExtendedQueryTags = true }).Load(builder.Services);
        }
    }
}
