// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Client.DurableTask;

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
        /// <paramref name="configurationRoot"/> is missing a section with the key TBD
        /// </para>
        /// </exception>
        public static IDicomServerBuilder AddAzureFunctionsClient(
            this IDicomServerBuilder dicomServerBuilder,
            IConfiguration configurationRoot)
        {
            EnsureArg.IsNotNull(dicomServerBuilder, nameof(dicomServerBuilder));
            EnsureArg.IsNotNull(configurationRoot, nameof(configurationRoot));

            IServiceCollection services = dicomServerBuilder.Services;
            services.TryAddSingleton<IGuidFactory, GuidFactory>();
            services.AddDurableClientFactory(x =>
            {
                x.IsExternalClient = true;
                x.TaskHub = null;
                x.ConnectionName = null;

            });

            return dicomServerBuilder;
        }
    }
}
