// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Extensions;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.DicomWeb.Service;
using Microsoft.Health.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Health.Client.Authentication;

namespace Microsoft.Extensions.DependencyInjection;

public static class DicomModulesExtensions
{
    private const string DicomWebConfigurationSectionName = "DicomWeb";

    public static IServiceCollection AddDicomModule(this IServiceCollection services, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        IConfigurationSection dicomWebConfigurationSection = configuration.GetSection(DicomWebConfigurationSectionName);
        services.AddOptions<DicomWebConfiguration>().Bind(dicomWebConfigurationSection);

        // Allow retries to occur catch 30 second outages
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // HttpRequestException, 5XX and 408
            .WaitAndRetryAsync(8, retryAttempt => retryAttempt <= 3 ? TimeSpan.FromSeconds(retryAttempt) : TimeSpan.FromSeconds(5));

        services.AddHttpClient<IDicomWebClient, DicomWebClient>(
            (httpClient, sp) =>
            {
                DicomWebConfiguration config = sp.GetRequiredService<IOptions<DicomWebConfiguration>>().Value;
                httpClient.BaseAddress = config.PrivateEndpoint == null ? config.Endpoint : config.PrivateEndpoint;
                return new DicomWebClient(httpClient, DicomApiVersions.V1);
            })
            .AddPolicyHandler(retryPolicy)
            .AddAuthenticationHandler(dicomWebConfigurationSection.GetSection(AuthenticationOptions.SectionName));

        services.Add<ChangeFeedRetrieveService>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();

        return services;
    }
}
