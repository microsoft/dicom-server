// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.Core.Modules;
using Microsoft.Health.DicomCast.TableStorage;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.DicomCast.Hosting;

public static class Program
{
    public static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, builder) =>
            {
                IConfiguration builtConfig = builder.Build();

                // TODO: Use Azure SDK directly for settings
                string keyVaultEndpoint = builtConfig["KeyVault:Endpoint"];
                if (!string.IsNullOrEmpty(keyVaultEndpoint))
                {
                    builder.AddAzureKeyVault(
                        new SecretClient(new Uri(keyVaultEndpoint), new DefaultAzureCredential()),
                        new AzureKeyVaultConfigurationOptions());
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = hostContext.Configuration;

                services.RegisterAssemblyModules(typeof(WorkerModule).Assembly, configuration);

                services.AddTableStorageDataStore(configuration);

                services.AddHostedService<DicomCastBackgroundService>();

                AddApplicationInsightsTelemetry(services, configuration);
            })
            .Build();

        host.Run();
    }

    /// <summary>
    /// Adds ApplicationInsights for telemetry and logging.
    /// </summary>
    private static void AddApplicationInsightsTelemetry(IServiceCollection services, IConfiguration configuration)
    {
        string instrumentationKey = configuration["ApplicationInsights:InstrumentationKey"];

        if (!string.IsNullOrWhiteSpace(instrumentationKey))
        {
            var connectionString = $"InstrumentationKey={instrumentationKey}";

            services.AddApplicationInsightsTelemetryWorkerService(aiServiceOptions => aiServiceOptions.ConnectionString = connectionString);
            services.AddLogging(
                loggingBuilder => loggingBuilder.AddApplicationInsights(
                    telemetryConfig => telemetryConfig.ConnectionString = connectionString,
                    aiLoggerOptions => { }
                ));
        }
    }
}
