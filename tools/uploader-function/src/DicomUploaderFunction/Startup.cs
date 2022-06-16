// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using DicomUploaderFunction.Configuration;
using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Client.Authentication;
using Microsoft.Health.Client.Extensions;
using Microsoft.Health.Dicom.Client;

[assembly: FunctionsStartup(typeof(DicomUploaderFunction.Startup))]

namespace DicomUploaderFunction;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));

        var configuration = builder.GetContext().Configuration;

        var dicomConfiguration = new DicomConfiguration();
        var dicomWebConfigurationSection = configuration.GetSection(DicomConfiguration.SectionName);
        dicomWebConfigurationSection.Bind(dicomConfiguration);

        builder.Services.AddHttpClient<IDicomWebClient, DicomWebClient>((sp, client) =>
            {
                client.BaseAddress = dicomConfiguration.Endpoint;
            })
            .AddAuthenticationHandler(dicomWebConfigurationSection.GetSection(AuthenticationOptions.SectionName));
    }
}
