// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Client.HealthChecks;
using Microsoft.Health.Dicom.Functions.Client.TaskHub;
using Microsoft.Health.Operations.Functions.DurableTask;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Functions.Client;

/// <summary>
/// Provides a set of <see langword="static"/> methods for adding services to the dependency injection
/// service container that are necessary for an Azure Functions-based <see cref="IDicomOperationsClient"/>
/// implementation.
/// </summary>
public static class DicomServerBuilderFunctionClientRegistrationExtensions
{
    /// <summary>
    /// Adds the necessary services to support the usage of <see cref="IDicomOperationsClient"/>.
    /// </summary>
    /// <param name="dicomServerBuilder">A service builder for constructing a DICOM server.</param>
    /// <param name="configuration">The root of a configuration containing settings for the client.</param>
    /// <param name="developmentEnvironment">If service is running in a development environment</param>
    /// <returns>The service builder for adding additional services.</returns>
    /// <exception cref="ArgumentNullException">
    /// <para>
    /// <paramref name="dicomServerBuilder"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </para>
    /// <para>-or-</para>
    /// <para>
    /// <paramref name="configuration"/> is missing a section with the key TBD
    /// </para>
    /// </exception>
    public static IDicomServerBuilder AddAzureFunctionsClient(
        this IDicomServerBuilder dicomServerBuilder,
        IConfiguration configuration,
        bool developmentEnvironment = false)
    {
        EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        IServiceCollection services = dicomServerBuilder.Services;
        services.AddOptions<DicomFunctionOptions>()
            .Bind(configuration.GetSection(DicomFunctionOptions.SectionName))
            .ValidateDataAnnotations();
        services.AddDurableClientFactory(
            x => configuration
                .GetSection(DicomFunctionOptions.SectionName)
                .GetSection(nameof(DicomFunctionOptions.DurableTask))
                .Bind(x));

        services.Configure<JsonSerializerSettings>(o => o.ConfigureDefaultDicomSettings());
        services.Replace(ServiceDescriptor.Singleton<IMessageSerializerSettingsFactory, MessageSerializerSettingsFactory>());
        services.TryAddScoped<IDicomOperationsClient, DicomAzureFunctionsClient>();

        services.AddAzureClientsCore();
        services.TryAddScoped<ITaskHubClient, AzureStorageTaskHubClient>();
        if (!developmentEnvironment)
        {
            services.AddHealthChecks().AddCheck<DurableTaskHealthCheck>("DurableTask");
        }

        return dicomServerBuilder;
    }
}
