// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnsureThat;
using Microsoft.ApplicationInsights.Extensibility;
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
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Features.BackgroundServices;
using Microsoft.Health.Dicom.Api.Features.Context;
using Microsoft.Health.Dicom.Api.Features.Conventions;
using Microsoft.Health.Dicom.Api.Features.Partitioning;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Features.Swagger;
using Microsoft.Health.Dicom.Api.Features.Telemetry;
using Microsoft.Health.Dicom.Api.Logging;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Extensions;
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
    /// <param name="configuration">The configuration for the DICOM server.</param>
    /// <returns>The DICOM server builder instance.</returns>
    public static IDicomServerBuilder AddBackgroundWorkers(this IDicomServerBuilder serverBuilder, IConfiguration configuration)
    {
        EnsureArg.IsNotNull(serverBuilder, nameof(serverBuilder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        FeatureConfiguration featureConfiguration = new FeatureConfiguration();
        configuration.GetSection("DicomServer").GetSection("Features").Bind(featureConfiguration);

        serverBuilder.Services.AddScoped<DeletedInstanceCleanupWorker>();
        serverBuilder.Services.AddHostedService<DeletedInstanceCleanupBackgroundService>();
        if (featureConfiguration.EnableExternalStore)
        {
            serverBuilder.Services.AddHostedService<StartContentLengthBackFillBackgroundService>();
        }

        HealthCheckPublisherConfiguration healthCheckPublisherConfiguration = new HealthCheckPublisherConfiguration();
        configuration.GetSection(HealthCheckPublisherConfiguration.SectionName).Bind(healthCheckPublisherConfiguration);
        IReadOnlyList<string> excludedHealthCheckNames = healthCheckPublisherConfiguration.GetListOfExcludedHealthCheckNames();

        serverBuilder.Services
            .AddCustomerKeyValidationBackgroundService(options => configuration
                .GetSection(CustomerManagedKeyOptions.CustomerManagedKey)
                .Bind(options))
            .AddHealthCheckCachePublisher(options =>
            {
                configuration
                    .GetSection(HealthCheckPublisherConfiguration.SectionName)
                    .Bind(options);

                options.Predicate = (check) => !excludedHealthCheckNames.Contains(check.Name);
            });

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

        var featuresOptions = Options.Create(dicomServerConfiguration.Features);
        services.AddSingleton(Options.Create(dicomServerConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Security));
        services.AddSingleton(featuresOptions);
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DeletedInstanceCleanup));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.StoreServiceSettings));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.ExtendedQueryTag));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DataPartition));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Audit));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Swagger));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.Retrieve));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.InstanceMetadataCacheConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.FramesRangeCacheConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.UpdateServiceSettings));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.DataCleanupConfiguration));
        services.AddSingleton(Options.Create(dicomServerConfiguration.Services.ContentLengthBackFillConfiguration));

        services.RegisterAssemblyModules(Assembly.GetExecutingAssembly(), dicomServerConfiguration);
        services.RegisterAssemblyModules(typeof(InitializationModule).Assembly, dicomServerConfiguration);
        services.AddApplicationInsightsTelemetry();

        services.AddOptions();

        services
            .AddControllers(options =>
            {
                options.EnableEndpointRouting = true;
                options.RespectBrowserAcceptHeader = true;
            })
            .AddJsonSerializerOptions(o => o.ConfigureDefaultDicomSettings());

        services.AddApiVersioning(c =>
        {
            c.ApiVersionReader = new UrlSegmentApiVersionReader();
            c.AssumeDefaultVersionWhenUnspecified = true;
            c.ReportApiVersions = true;
            c.UseApiBehavior = false;

            c.Conventions.Add(new ApiVersionsConvention(featuresOptions));
        });

        services.AddVersionedApiExplorer(options =>
        {
            // The format for this is 'v'major[.minor][-status] ex. v1.0-prerelease
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();
            options.OperationFilter<ErrorCodeOperationFilter>();
            options.OperationFilter<RetrieveOperationFilter>();
            options.DocumentFilter<ReflectionTypeFilter>();
            options.SchemaFilter<IgnoreEnumSchemaFilter>();
        });

        services.AddSingleton<IUrlResolver, UrlResolver>();

        services.RegisterAssemblyModules(typeof(DicomMediatorExtensions).Assembly, dicomServerConfiguration.Features, dicomServerConfiguration.Services);
        services.AddTransient<IStartupFilter, DicomServerStartupFilter>();

        services.AddRecyclableMemoryStreamManager(configurationRoot);

        services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
        services.AddSingleton<IDicomTelemetryClient, HttpDicomTelemetryClient>();

        CustomDicomImplementation.SetDicomImplementationClassUIDAndVersion();

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
