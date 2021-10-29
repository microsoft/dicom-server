// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Text.Json.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Api.Features.Context;
using Microsoft.Health.Api.Features.Cors;
using Microsoft.Health.Api.Features.Headers;
using Microsoft.Health.Api.Modules;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Features.BackgroundServices;
using Microsoft.Health.Dicom.Api.Features.Context;
using Microsoft.Health.Dicom.Api.Features.Formatters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Features.Swagger;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Builder
{
    public static class DicomServerServiceCollectionExtensions
    {
        private const string DicomServerConfigurationSectionName = "DicomServer";

        /// <summary>
        /// Add services for DICOM background workers.
        /// </summary>
        /// <param name="serverBuilder">The DICOM server builder instance.</param>
        /// <returns>The DICOM server builder instance.</returns>
        public static IDicomServerBuilder AddBackgroundWorkers(this IDicomServerBuilder serverBuilder)
        {
            EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
            serverBuilder.Services.AddScoped<DeletedInstanceCleanupWorker>();
            serverBuilder.Services.AddHostedService<DeletedInstanceCleanupBackgroundService>();
            return serverBuilder;
        }

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
            services.AddSingleton(Options.Create(dicomServerConfiguration.Features));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DeletedInstanceCleanup));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Services.StoreServiceSettings));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Services.ExtendedQueryTag));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DataPartition));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Audit));
            services.AddSingleton(Options.Create(dicomServerConfiguration.Swagger));

            services.RegisterAssemblyModules(Assembly.GetExecutingAssembly(), dicomServerConfiguration);
            services.RegisterAssemblyModules(typeof(InitializationModule).Assembly, dicomServerConfiguration);
            services.AddApplicationInsightsTelemetry();

            services.AddOptions();

            services
                .AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                    options.RespectBrowserAcceptHeader = true;
                    options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter());
                })
                .AddJsonSerializerOptions(o => o.Converters.Add(new JsonStringEnumConverter()));

            services.AddApiVersioning(c =>
            {
                c.ApiVersionReader = new UrlSegmentApiVersionReader();
                c.AssumeDefaultVersionWhenUnspecified = true;
                c.DefaultApiVersion = new ApiVersion(1, 0, "prerelease");
                c.ReportApiVersions = true;
                c.UseApiBehavior = false;
            });

            services.AddVersionedApiExplorer(options =>
            {
                // The format for this is 'v'major[.minor][-status] ex. v1.0-prerelease
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(options => options.OperationFilter<SwaggerDefaultValues>());
            services.AddSwaggerGen(options => options.OperationFilter<ErrorCodeOperationFilter>());
            services.AddSwaggerGen(options => options.OperationFilter<RetrieveOperationFilter>());
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddSingleton<IUrlResolver, UrlResolver>();

            services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly, dicomServerConfiguration.Features, dicomServerConfiguration.Services);
            services.AddTransient<IStartupFilter, DicomServerStartupFilter>();

            // Register the Json Serializer to use
            services.AddDicomJsonNetSerialization();

            services.TryAddSingleton<RecyclableMemoryStreamManager>();

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
                    IWebHostEnvironment env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

                    IApiVersionDescriptionProvider provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

                    // This middleware will add delegates to the OnStarting method of httpContext.Response for setting headers.
                    app.UseBaseHeaders();

                    app.UseCors(CorsConstants.DefaultCorsPolicy);

                    app.UseDicomRequestContext();

                    if (env.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }

                    app.UseAudit();

                    app.UseExceptionHandling();

                    app.UseAuthentication();

                    app.UseRequestContextAfterAuthentication<IDicomRequestContext>();

                    app.UseSwagger(c =>
                    {
                        c.RouteTemplate = "{documentName}/api.{json|yaml}";
                    });

                    //Disabling swagger ui until accesability team gets back to us
                    /*app.UseSwaggerUI(options =>
                    {
                        foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                        {
                            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.yaml", description.GroupName.ToUpperInvariant());
                        }
                    });*/

                    next(app);
                };
            }
        }
    }
}
