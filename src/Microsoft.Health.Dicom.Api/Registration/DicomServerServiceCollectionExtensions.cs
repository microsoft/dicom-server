// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Text.Json.Serialization;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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
using Microsoft.Health.Dicom.Api.Registration.Swagger;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;
using Newtonsoft.Json;
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
            services.AddSingleton(Options.Create(dicomServerConfiguration.Audit));

            services.RegisterAssemblyModules(Assembly.GetExecutingAssembly(), dicomServerConfiguration);
            services.RegisterAssemblyModules(typeof(InitializationModule).Assembly, dicomServerConfiguration);
            services.AddApplicationInsightsTelemetry();

            services.AddOptions();

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                options.RespectBrowserAcceptHeader = true;
                options.OutputFormatters.Insert(0, new DicomJsonOutputFormatter());
            }).AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddApiVersioning(c =>
            {
                c.AssumeDefaultVersionWhenUnspecified = true;
                c.DefaultApiVersion = new ApiVersion(1, 0, "prerelease");
                c.ReportApiVersions = true;
                c.UseApiBehavior = false;
            });

            services.AddApiVersioning();
            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(options => options.OperationFilter<SwaggerDefaultValues>());

            services.AddSingleton<IUrlResolver, UrlResolver>();

            services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly, dicomServerConfiguration.Features);
            services.AddTransient<IStartupFilter, DicomServerStartupFilter>();

            // Register the Json Serializer to use
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.Converters.Add(new JsonDicomConverter());
            services.AddSingleton(jsonSerializer);

            services.TryAddSingleton<RecyclableMemoryStreamManager>();

            // Disable fo-dicom data item validation. Disabling at global level
            // Opt-in validation instead of opt-out
            // De-serializing to Dataset while read has no Dataset level option to disable validation
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

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

                    app.UseSwagger();

                    app.UseSwaggerUI(options =>
                    {
                        foreach (var description in provider.ApiVersionDescriptions)
                        {
                            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                        }
                    });

                    next(app);
                };
            }
        }
    }
}
