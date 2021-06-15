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
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Dicom.Operations.Functions.Registration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add services for DICOM functions.
        /// </summary>
        /// <param name="services">The DICOM function builder instance.</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The DICOM function builder instance.</returns>
        public static IDicomFunctionsBuilder AddDicomFunctions(this IServiceCollection services, IConfiguration configuration)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            DicomFunctionsConfiguration dicomOperationsConfig = new DicomFunctionsConfiguration();
            configuration?.GetSection(DicomFunctionsConfiguration.SectionName).Bind(dicomOperationsConfig);
            services.AddSingleton(Options.Create(dicomOperationsConfig));

            services.AddMvcCore()
                .AddNewtonsoftJson(x => x.SerializerSettings.Converters
                    .Add(new StringEnumConverter()));

            return new DicomFunctionsBuilder(services);
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
