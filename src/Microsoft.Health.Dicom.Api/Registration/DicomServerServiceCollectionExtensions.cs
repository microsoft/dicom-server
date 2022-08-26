// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using EnsureThat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using Microsoft.Health.Dicom.Api.Features.Partition;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Features.Swagger;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Builder;

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
        serverBuilder.Services.AddHostedService<StartBlobMigrationService>();
        serverBuilder.Services.AddHostedService<StartBlobDeleteMigrationService>();
        return serverBuilder;
    }

    /// <summary>
    /// Add services for DICOM hosted services.
    /// </summary>
    /// <param name="serverBuilder">The DICOM server builder instance.</param>
    /// <returns>The DICOM server builder instance.</returns>
    public static IDicomServerBuilder AddHostedServices(this IDicomServerBuilder serverBuilder)
    {
        EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
        serverBuilder.Services.AddHostedService<DataPartitionFeatureValidatorService>();
        return serverBuilder;
    }

    /// <summary>
    /// Adds services for enabling a DICOMWeb API.
    /// </summary>
    /// <param name="dicomServerBuilder">The services collection.</param>
    /// <param name="configurationRoot">An optional configuration root object. This method uses the "DicomServer" section.</param>
    /// <param name="configureAction">An optional delegate to set <see cref="DicomApiConfiguration"/> properties after values have been loaded from configuration.</param>
    /// <returns>A <see cref="IDicomServerBuilder"/> object.</returns>
    public static IDicomServerBuilder AddWebApi(
        this IDicomServerBuilder dicomServerBuilder,
        IConfiguration configurationRoot,
        Action<DicomApiConfiguration> configureAction = null)
    {
        EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));

        var dicomApiConfiguration = new DicomApiConfiguration();

        configurationRoot?.GetSection(DicomServerConfigurationSectionName).Bind(dicomApiConfiguration);
        configureAction?.Invoke(dicomApiConfiguration);

        dicomServerBuilder.Services.AddSingleton(Options.Create(dicomApiConfiguration));
        dicomServerBuilder.Services.AddSingleton(Options.Create(dicomApiConfiguration.Swagger));

        // Register modules in Microsoft.Health.Dicom.Api
        dicomServerBuilder.Services.RegisterAssemblyModules(Assembly.GetExecutingAssembly(), dicomServerBuilder.DicomServerConfiguration);

        // Register modules in Microsoft.Health.Api
        dicomServerBuilder.Services.RegisterAssemblyModules(typeof(InitializationModule).Assembly, dicomServerBuilder.DicomServerConfiguration, dicomApiConfiguration);
        dicomServerBuilder.Services.AddApplicationInsightsTelemetry();

        dicomServerBuilder.Services.AddOptions();

        dicomServerBuilder.Services
            .AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
                options.RespectBrowserAcceptHeader = true;
            })
            .AddJsonSerializerOptions(o => o.ConfigureDefaultDicomSettings());

        dicomServerBuilder.Services.AddApiVersioning(c =>
        {
            c.ApiVersionReader = new UrlSegmentApiVersionReader();
            c.AssumeDefaultVersionWhenUnspecified = true;
            c.ReportApiVersions = true;
            c.UseApiBehavior = false;
        });

        dicomServerBuilder.Services.AddVersionedApiExplorer(options =>
        {
            // The format for this is 'v'major[.minor][-status] ex. v1.0-prerelease
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        dicomServerBuilder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        dicomServerBuilder.Services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();
            options.OperationFilter<ErrorCodeOperationFilter>();
            options.OperationFilter<RetrieveOperationFilter>();
            options.DocumentFilter<ReflectionTypeFilter>();
        });

        dicomServerBuilder.Services.AddSingleton<IUrlResolver, UrlResolver>();

        dicomServerBuilder.Services.AddTransient<IStartupFilter, DicomServerStartupFilter>();

        return dicomServerBuilder;
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

                // Dependency on URSA scan. We should see how other teams do this.
                app.UseSwagger(c =>
                {
                    c.RouteTemplate = "{documentName}/api.{json|yaml}";
                });

                //Disabling swagger ui until accesability team gets back to us
                //app.UseSwaggerUI(options =>
                //{
                //    foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                //    {
                //        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.yaml", description.GroupName.ToUpperInvariant());
                //    }
                //});

                next(app);
            };
        }
    }
}
