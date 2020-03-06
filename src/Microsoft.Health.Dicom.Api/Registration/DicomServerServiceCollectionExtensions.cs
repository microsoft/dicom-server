// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerServiceCollectionExtensions
    {
        private const string DicomServerConfigurationSectionName = "DicomServer";

        /// <summary>
        /// Adds services for enabling a DICOM server.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="configurationRoot">An optional configuration root object. This method uses the "DicomServer" section.</param>
        /// <param name="configureAction">An optional delegate to set <see cref="DicomServerConfiguration"/> properties after values have been loaded from configuration.</param>
        /// <returns>A <see cref="IDicomServerBuilder"/> object.</returns>
        public static IDicomServerBuilder AddDicomServer(
            this IServiceCollection services,
            IConfiguration configurationRoot,
            Action<DicomServerConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            var dicomServerConfiguration = new DicomServerConfiguration();

            configurationRoot?.GetSection(DicomServerConfigurationSectionName).Bind(dicomServerConfiguration);
            configureAction?.Invoke(dicomServerConfiguration);

            services.AddSingleton(Options.Create(dicomServerConfiguration));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Security));

            services.RegisterAssemblyModules(Assembly.GetExecutingAssembly(), dicomServerConfiguration);

            services.AddOptions();

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                options.RespectBrowserAcceptHeader = true;
                options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter());

                if (!dicomServerConfiguration.Security.Enabled)
                {
                    // Removes Authentication Requirements for all endpoints
                    options.Filters.Add(new AllowAnonymousFilter());
                }
            });

            services.AddSingleton<IDicomRouteProvider, DicomRouteProvider>();
            services.Add<DicomDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly, dicomServerConfiguration);
            services.AddTransient<IStartupFilter, DicomServerStartupFilter>();

            services.AddTransient<IDicomQueryParser, DicomQueryParser>();

            // Register the Json Serializer to use
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());
            services.AddSingleton(jsonSerializer);

            services.AddSingleton<RecyclableMemoryStreamManager>();
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

        /// <summary>
        /// An <see cref="IStartupFilter"/> that configures middleware components before any components are added in Startup.Configure
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated.")]
        private class DicomServerStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.UseExceptionHandling();

                    app.UseAuthentication();

                    next(app);
                };
            }
        }
    }
}
