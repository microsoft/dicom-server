// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Azure.Monitor.OpenTelemetry.Exporter;
using EnsureThat;
using FellowOakDicom;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Registration;
using Microsoft.Health.Development.IdentityProvider.Registration;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Api.Registration;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Functions.Client;
using Microsoft.Health.Dicom.SqlServer.Registration;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Health.Dicom.Web;

public class Startup
{
    private readonly IWebHostEnvironment _environment;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        _environment = environment;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void ConfigureServices(IServiceCollection services)
    {
        services.Configure<IISServerOptions>(options =>
        {
            // When hosted on IIS, the max request body size can not over 2GB, according to Asp.net Core bug https://github.com/dotnet/aspnetcore/issues/2711
            options.MaxRequestBodySize = int.MaxValue;
        });
        services.AddDevelopmentIdentityProvider<DataActions>(Configuration, "DicomServer");

        // The execution of IHostedServices depends on the order they are added to the dependency injection container, so we
        // need to ensure that the schema is initialized before the background workers are started.
        services.AddDicomServer(Configuration)
            .AddBlobDataStores(Configuration)
            .AddSqlServer(Configuration)
            .AddKeyVaultClient(Configuration)
            .AddAzureFunctionsClient(Configuration, _environment.IsDevelopment())
            .AddBackgroundWorkers(Configuration)
            .AddHostedServices();

        AddOpenTelemetryMetrics(services);
        AddApplicationInsightsTelemetry(services);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app)
    {
        EnsureArg.IsNotNull(app, nameof(app));

        app.UseQueryStringValidator();

        app.UseHttpsRedirection();

        app.UseCachedHealthChecks(new PathString(KnownRoutes.HealthCheck));

        // Update Fellow Oak DICOM services to use ASP.NET Core's service container
        DicomSetupBuilder.UseServiceProvider(app.ApplicationServices);

        IOptions<FeatureConfiguration> featureConfiguration = app.ApplicationServices.GetRequiredService<IOptions<FeatureConfiguration>>();
        if (featureConfiguration.Value.EnableOhifViewer)
        {
            app.UseOhifViewer();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }

    /// <summary>
    /// Adds Open Telemetry metrics.
    /// </summary>
    private void AddOpenTelemetryMetrics(IServiceCollection services)
    {
        var builder = Sdk.CreateMeterProviderBuilder()
            .AddMeter($"{OpenTelemetryLabels.BaseMeterName}.*");

        if (_environment.IsDevelopment())
        {
            builder.AddConsoleExporter();
        }

        string instrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
        if (!string.IsNullOrWhiteSpace(instrumentationKey))
        {
            var connectionString = $"InstrumentationKey={instrumentationKey}";
            builder.AddAzureMonitorMetricExporter(o => o.ConnectionString = connectionString);
        }

        services.AddSingleton(builder.Build());
    }

    /// <summary>
    /// Adds ApplicationInsights for logging.
    /// </summary>
    private void AddApplicationInsightsTelemetry(IServiceCollection services)
    {
        string instrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
        if (!string.IsNullOrWhiteSpace(instrumentationKey))
        {
            var connectionString = $"InstrumentationKey={instrumentationKey}";
            services.AddApplicationInsightsTelemetry(aiOptions => aiOptions.ConnectionString = connectionString);
        }
    }
}
