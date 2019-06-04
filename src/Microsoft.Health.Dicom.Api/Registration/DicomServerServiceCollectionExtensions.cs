// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerServiceCollectionExtensions
    {
        private const string DicomServerConfigurationSectionName = "DicomServer";

        /// <summary>
        /// Adds services for enabling a FHIR server.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="configurationRoot">An optional configuration root object. This method uses "FhirServer" section.</param>
        /// <param name="configureAction">An optional delegate to set <see cref="FhirServerConfiguration"/> properties after values have been loaded from configuration</param>
        /// <returns>A <see cref="IFhirServerBuilder"/> object.</returns>
        public static IDicomServerBuilder AddDicomServer(
            this IServiceCollection services,
            IConfiguration configurationRoot = null,
            Action<DicomServerConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddOptions();
            services.AddMvc(options =>
            {
                options.RespectBrowserAcceptHeader = true;
                options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter());
            });

            var serverConfiguration = new DicomServerConfiguration();

            configurationRoot?.GetSection(DicomServerConfigurationSectionName).Bind(serverConfiguration);
            configureAction?.Invoke(serverConfiguration);

            services.AddSingleton(Options.Create(serverConfiguration));

            services.AddSingleton<TextOutputFormatter>(new DicomJsonOutputFormatter());
            services.AddSingleton<IDicomRouteProvider>(new DicomRouteProvider());

            services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly, serverConfiguration);

            return new FhirServerBuilder(services);
        }

        private class FhirServerBuilder : IDicomServerBuilder
        {
            public FhirServerBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
