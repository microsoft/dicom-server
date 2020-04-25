// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using EnsureThat;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Core.Modules
{
    public class MediationModule : IStartupModule
    {
        /// <inheritdoc />
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            Assembly coreAssembly = typeof(MediationModule).Assembly;

            services.AddMediatR(GetType().Assembly, coreAssembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));

            services.TypesInSameAssemblyAs<MediationModule>()
                .Transient()
                .AsSelf()
                .AsImplementedInterfaces(IsPipelineBehavior);

            var openRequestInterfaces = new Type[] { typeof(IRequestHandler<,>) };

            services.TypesInSameAssemblyAs<MediationModule>()
                .Where(y => y.Type.IsGenericType && openRequestInterfaces.Contains(y.Type.GetGenericTypeDefinition()))
                .Transient();
        }

        private static bool IsPipelineBehavior(Type t)
            => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
    }
}
