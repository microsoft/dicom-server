// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Microsoft.Health.Extensions.DependencyInjection;
using FhirClient = Microsoft.Health.Fhir.Client.FhirClient;
using IFhirClient = Microsoft.Health.Fhir.Client.IFhirClient;

namespace Microsoft.Health.DicomCast.Core.Modules
{
    public class FhirModule : IStartupModule
    {
        private const string FhirConfigurationSectionName = "Fhir";

        private readonly IConfiguration _configuration;

        public FhirModule(IConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            FhirConfiguration fhirConfiguration = services.Configure<FhirConfiguration>(
                _configuration,
                FhirConfigurationSectionName);

            services.AddHttpClient<IFhirClient, FhirClient>(sp =>
                {
                    sp.BaseAddress = fhirConfiguration.Endpoint;
                })
                .AddAuthenticationHandler(services, fhirConfiguration.Authentication, FhirConfigurationSectionName);

            services.Add<FhirResourceValidator>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<FhirService>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<FhirTransactionExecutor>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ImagingStudyUpsertHandler>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ImagingStudyDeleteHandler>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ObservationUpsertHandler>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<ObservationDeleteHandler>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();
        }
    }
}
