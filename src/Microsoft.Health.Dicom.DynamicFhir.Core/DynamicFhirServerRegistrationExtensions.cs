// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Resources.Create;
using Microsoft.Health.Fhir.Core.Features.Resources.Delete;
using Microsoft.Health.Fhir.Core.Features.Resources.Get;
using Microsoft.Health.Fhir.Core.Features.Resources.Upsert;

namespace Microsoft.Health.Dicom.DynamicFhir.Core
{
    public static class DynamicFhirServerRegistrationExtensions
    {
        public static IServiceCollection ReplaceConfiguredConformanceProvider(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            var serviceDescriptor = services.FirstOrDefault(service => service.ServiceType == typeof(IConfiguredConformanceProvider));
            services.Remove(serviceDescriptor);

            // Add conformance provider for implementation metadata.
            services.AddSingleton<IConfiguredConformanceProvider, DynamicFhirConfiguredConformanceProvider>();
            return services;
        }

        public static IServiceCollection ReplaceResourceHandlers(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            var typesToRemove = new[]
            {
                typeof(CreateResourceHandler),
                typeof(DeleteResourceHandler),
                typeof(UpsertResourceHandler),
                typeof(GetResourceHandler),
            };

            foreach (var typeToRemove in typesToRemove)
            {
                var serviceDescriptors = services.Where(service => service.ImplementationType == typeToRemove).ToArray();
                foreach (var serviceToRemove in serviceDescriptors)
                {
                    services.Remove(serviceToRemove);
                }
            }

            services.Add<FhirDicomStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<DynamicFhirGetResourceHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }
    }
}
