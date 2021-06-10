// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Registration;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;

namespace Microsoft.Health.Dicom.Functions.Registration
{
    public static class DicomFunctionsServiceCollectionExtensions
    {
        /// <summary>
        /// Add services for DICOM background workers.
        /// </summary>
        /// <param name="services">The DICOM server builder instance.</param>
        /// <param name="configuration"></param>
        /// <returns>The DICOM server builder instance.</returns>
        public static IDicomFunctionsBuilder AddDicomFunctions(this IServiceCollection services, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            DicomFunctionsConfiguration dicomFunctionsConfig = new DicomFunctionsConfiguration();
            configuration?.GetSection(DicomFunctionsConfiguration.SectionName).Bind(dicomFunctionsConfig);
            services.AddSingleton(Options.Create(dicomFunctionsConfig));
            return new DicomFunctionsBuilder(services)
                .AddCoreComponents();
        }

        private class DicomFunctionsBuilder : IDicomFunctionsBuilder
        {
            public DicomFunctionsBuilder(IServiceCollection services)
            {
                EnsureArg.IsNotNull(services, nameof(services));
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
