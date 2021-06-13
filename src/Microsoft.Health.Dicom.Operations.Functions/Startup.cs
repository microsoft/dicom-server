// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Operations.Functions.Indexing.Configuration;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Dicom.Operations.Functions.Startup))]
namespace Microsoft.Health.Dicom.Operations.Functions
{
    public class Startup : FunctionsStartup
    {
        private const string HostSectionName = "AzureFunctionsJobHost";

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services
                .AddOptions<IndexingConfiguration>()
                .Configure<IConfiguration>((sectionObj, config) => config
                    .GetSection(HostSectionName)
                    .GetSection(IndexingConfiguration.SectionName)
                    .Bind(sectionObj));
        }
    }
}
