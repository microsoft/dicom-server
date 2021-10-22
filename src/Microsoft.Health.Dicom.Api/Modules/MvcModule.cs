// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Api.Modules
{
    public class MvcModule : IStartupModule
    {
        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.PostConfigure<MvcOptions>(options =>
            {
                // This filter should run first because it populates data for DicomRequestContext.
                options.Filters.Add(typeof(DicomRequestContextRouteDataPopulatingFilterAttribute), 0);
            });

            services.AddHttpContextAccessor();

            // These are needed for IUrlResolver. If it's no longer need it,
            // we should remove the registration since enabling these accessors has performance implications.
            // https://github.com/aspnet/Hosting/issues/793
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.Add<PopulateDataPartitionFilterAttribute>()
                .Singleton()
                .AsService<PopulateDataPartitionFilterAttribute>();
        }
    }
}
