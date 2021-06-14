// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Operations.Functions.Configs;

namespace Microsoft.Health.Dicom.Operations.Functions.Registration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add services for DICOM operations.
        /// </summary>
        /// <param name="services">The DICOM function builder instance.</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The DICOM function builder instance.</returns>
        public static IDicomOperationsBuilder AddDicomOperations(this IServiceCollection services, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            DicomOperationsConfiguration dicomOperationsConfig = new DicomOperationsConfiguration();
            configuration?.GetSection(DicomOperationsConfiguration.SectionName).Bind(dicomOperationsConfig);
            services.AddSingleton(Options.Create(dicomOperationsConfig));
            return new DicomOperationsBuilder(services);
        }

        private class DicomOperationsBuilder : IDicomOperationsBuilder
        {
            public DicomOperationsBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
