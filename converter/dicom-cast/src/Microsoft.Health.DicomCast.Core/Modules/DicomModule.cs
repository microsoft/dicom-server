// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Health.DicomCast.Core.Modules;

public class DicomModule : IStartupModule
{
    private const string DicomWebConfigurationSectionName = "DicomWeb";

    private readonly IConfiguration _configuration;

    public DicomModule(IConfiguration configuration)
    {
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        _configuration = configuration;
    }

    public void Load(IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        IConfigurationSection dicomWebConfigurationSection = _configuration.GetSection(DicomWebConfigurationSectionName);
        services.AddOptions<DicomWebConfiguration>().Bind(dicomWebConfigurationSection);

        // Allow retries to occur catch 30 second outages
        var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException, 5XX and 408
                .WaitAndRetryAsync(8, retryAttempt => retryAttempt > 3 ? TimeSpan.FromSeconds(retryAttempt) : TimeSpan.FromSeconds(5));

        services.AddHttpClient<IDicomWebClient, DicomWebClient>((sp, client) =>
            {
                DicomWebConfiguration config = sp.GetRequiredService<IOptions<DicomWebConfiguration>>().Value;
                client.BaseAddress = config.PrivateEndpoint == null ? config.Endpoint : config.PrivateEndpoint;
            })
            .AddPolicyHandler(retryPolicy)
            .AddAuthenticationHandler(services, dicomWebConfigurationSection.GetSection(AuthenticationConfiguration.SectionName), DicomWebConfigurationSectionName);

        services.Add<ChangeFeedRetrieveService>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();
    }
}
