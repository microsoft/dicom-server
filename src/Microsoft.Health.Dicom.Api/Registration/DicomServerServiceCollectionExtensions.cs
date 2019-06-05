// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services for enabling a FHIR server.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <returns>A <see cref="IDicomServerBuilder"/> object.</returns>
        public static IDicomServerBuilder AddDicomServer(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddOptions();
            services.AddMvc(options =>
            {
                options.RespectBrowserAcceptHeader = true;
                options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter());
            });

            services.AddSingleton<TextOutputFormatter>(new DicomJsonOutputFormatter());
            services.AddSingleton<IDicomRouteProvider>(new DicomRouteProvider());

            services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly);

            return new DicomServerBuilder(services);
        }

        private class DicomServerBuilder : IDicomServerBuilder
        {
            public DicomServerBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
