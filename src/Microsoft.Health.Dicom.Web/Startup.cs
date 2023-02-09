// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Development.IdentityProvider.Registration;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Features.Telemetry;
using Microsoft.Health.Dicom.Functions.Client;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Health.Dicom.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
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
            .AddAzureFunctionsClient(Configuration)
            .AddBackgroundWorkers()
            .AddHostedServices();

        AddTelemetry(services);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseDicomServer();

        app.UseDevelopmentIdentityProviderIfConfigured();
    }

    private void AddTelemetry(IServiceCollection services)
    {
        string instrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];

        if (!string.IsNullOrWhiteSpace(instrumentationKey))
        {
            var connectionString = $"InstrumentationKey={instrumentationKey}";
            AddOpenTelemetryMetrics(services, connectionString);
            AddApplicationInsightsTelemetry(services, connectionString);
        }
    }

    /// <summary>
    /// Adds Open telemetry exporter for Azure monitor.
    /// </summary>
    private static void AddOpenTelemetryMetrics(IServiceCollection services, string connectionString)
    {
        services.AddSingleton(Sdk.CreateMeterProviderBuilder()
            .AddMeter($"{OpenTelemetryLabels.BaseMeterName}.*")
            .AddAzureMonitorMetricExporter(o => o.ConnectionString = connectionString)
            .Build());
    }

    /// <summary>
    /// Adds ApplicationInsights for logging.
    /// </summary>
    private static void AddApplicationInsightsTelemetry(IServiceCollection services, string connectionString)
    {
        services.AddApplicationInsightsTelemetry(aiOptions => aiOptions.ConnectionString = connectionString);
    }
}
