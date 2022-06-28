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

internal static class DicomSourceStoreExtension
{
    private const string DicomWebConfigurationSectionName = "DicomWeb";

    public static IServiceCollection AddDicomModule(this IServiceCollection services, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        IConfigurationSection dicomWebConfigurationSection = configuration.GetSection(DicomWebConfigurationSectionName);
        services.AddOptions<DicomWebConfiguration>().Bind(dicomWebConfigurationSection);

        // Allow retries to occur catch 30 second outages
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // HttpRequestException, 5XX and 408
            .WaitAndRetryAsync(8, retryAttempt => retryAttempt <= 3 ? TimeSpan.FromSeconds(retryAttempt) : TimeSpan.FromSeconds(5));

        services.AddHttpClient<IDicomWebClient, DicomWebClient>((sp, client) =>
        {
            DicomWebConfiguration config = sp.GetRequiredService<IOptions<DicomWebConfiguration>>().Value;
            client.BaseAddress = config.PrivateEndpoint == null ? config.Endpoint : config.PrivateEndpoint;
        })
            .AddPolicyHandler(retryPolicy)
            .AddAuthenticationHandler(services, dicomWebConfigurationSection.GetSection(AuthenticationConfiguration.SectionName), DicomWebConfigurationSectionName);

        return (IServiceCollection)services.Add<ChangeFeedRetrieveService>()
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();
    }
}
