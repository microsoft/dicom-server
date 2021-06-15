// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Microsoft.Health.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Microsoft.Health.Dicom.Functions.Client
{
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
        /// <param name="configurationRoot">The root of a configuration containing settings for the client.</param>
        /// <param name="configureAction">An optional delegate for modifying the resulting configuration.</param>
        /// <returns>The service builder for adding additional services.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>
        /// <paramref name="dicomServerBuilder"/> or <paramref name="configurationRoot"/> is <see langword="null"/>.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        /// <paramref name="configurationRoot"/> is missing a section with the key
        /// <see cref="FunctionsClientConfiguration.SectionName"/>.
        /// </para>
        /// </exception>
        public static IDicomServerBuilder AddAzureFunctionsClient(
            this IDicomServerBuilder dicomServerBuilder,
            IConfiguration configurationRoot,
            Action<FunctionsClientConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
            EnsureArg.IsNotNull(configurationRoot, nameof(configurationRoot));

            IServiceCollection services = dicomServerBuilder.Services;

            FunctionsClientConfiguration config = configurationRoot
                .GetSection(FunctionsClientConfiguration.SectionName)
                .Get<FunctionsClientConfiguration>();

            EnsureArg.IsNotNull(config, nameof(configurationRoot));

            configureAction?.Invoke(config);
            services.AddSingleton(Options.Create(config));

            IEnumerable<TimeSpan> delays = Backoff.ExponentialBackoff(
                TimeSpan.FromMilliseconds(config.MinRetryDelayMilliseconds),
                config.MaxRetries);

            services.AddHttpClient<DicomAzureFunctionsHttpClient>()
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(delays));

            services.Add<DicomAzureFunctionsHttpClient>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return dicomServerBuilder;
        }
    }
}
