// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Client.Configs;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

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
        /// <returns>The service builder for adding additional services.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>
        /// <paramref name="dicomServerBuilder"/> or <paramref name="configurationRoot"/> is <see langword="null"/>.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        /// <paramref name="configurationRoot"/> is missing a section with the key
        /// <see cref="FunctionsClientOptions.SectionName"/>.
        /// </para>
        /// </exception>
        public static IDicomServerBuilder AddAzureFunctionsClient(
            this IDicomServerBuilder dicomServerBuilder,
            IConfiguration configurationRoot)
        {
            EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
            EnsureArg.IsNotNull(configurationRoot, nameof(configurationRoot));

            IServiceCollection services = dicomServerBuilder.Services;

            services.TryAddScoped<IDicomOperationsClient, DicomAzureFunctionsHttpClient>();
            services
                .AddHttpClient<DicomAzureFunctionsHttpClient>()
                .AddHttpMessageHandler(
                    sp =>
                    {
                        FunctionsClientOptions config = sp.GetRequiredService<IOptions<FunctionsClientOptions>>().Value;
                        IEnumerable<TimeSpan> delays = Backoff.ExponentialBackoff(
                            TimeSpan.FromMilliseconds(config.MinRetryDelayMilliseconds),
                            config.MaxRetries);

                        IAsyncPolicy<HttpResponseMessage> policy = HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .WaitAndRetryAsync(delays);

                        return new PolicyHttpMessageHandler(policy);
                    });
            services.Configure<FunctionsClientOptions>(configurationRoot.GetSection(FunctionsClientOptions.SectionName));

            return dicomServerBuilder;
        }
    }
}
