// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Api.Controllers;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class StartupBaseForCustomProviders : Startup
    {
        public StartupBaseForCustomProviders(IConfiguration configuration)
            : base(configuration)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddMvc()
                .AddApplicationPart(typeof(RetrieveController).Assembly);
        }
    }
}
