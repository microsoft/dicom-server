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

        IConfiguration configuration = builder.GetContext().Configuration;

        var dicomOptions = new DicomOptions();
        IConfigurationSection dicomWebConfigurationSection = configuration.GetSection(DicomOptions.SectionName);
        dicomWebConfigurationSection.Bind(dicomOptions);

        builder.Services.AddHttpClient<IDicomWebClient, DicomWebClient>((sp, client) => client.BaseAddress = dicomOptions.Endpoint)
            .AddAuthenticationHandler(dicomWebConfigurationSection.GetSection(AuthenticationOptions.SectionName));
    }
}
