// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Dicom.Api.Modules
{
    public class SecurityModule : IStartupModule
    {
        private readonly SecurityConfiguration _securityConfiguration;

        public SecurityModule(DicomServerConfiguration dicomServerConfiguration)
        {
            EnsureArg.IsNotNull(dicomServerConfiguration, nameof(dicomServerConfiguration));
            _securityConfiguration = dicomServerConfiguration.Security;
        }

        public void Load(IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            if (_securityConfiguration.Enabled)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                    .AddJwtBearer(options =>
                    {
                        options.Authority = _securityConfiguration.Authentication.Authority;
                        options.Audience = _securityConfiguration.Authentication.Audience;
                        options.RequireHttpsMetadata = true;
                    });
            }
        }
    }
}
