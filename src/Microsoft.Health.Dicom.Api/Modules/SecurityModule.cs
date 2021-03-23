// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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

            // Set the token handler to not do auto inbound mapping. (e.g. "roles" -> "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (_securityConfiguration.Enabled)
            {
                string[] validAudiences = GetValidAudiences();
                string challengeAudience = validAudiences?.FirstOrDefault();

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = _securityConfiguration.Authentication.Authority;
                    options.RequireHttpsMetadata = true;
                    options.Challenge = $"Bearer authorization_uri=\"{_securityConfiguration.Authentication.Authority}\", resource_id=\"{challengeAudience}\", realm=\"{challengeAudience}\"";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudiences = validAudiences,
                    };
                });

                services.AddControllers(mvcOptions =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();

                    mvcOptions.Filters.Add(new AuthorizeFilter(policy));
                });

                if (_securityConfiguration.Authorization.Enabled)
                {
                    services.Add<DicomRoleLoader>().Transient().AsImplementedInterfaces();
                    services.AddSingleton(_securityConfiguration.Authorization);

                    services.AddSingleton<IAuthorizationService<DataActions>, RoleBasedAuthorizationService<DataActions, IDicomRequestContext>>();
                }
                else
                {
                    services.AddSingleton<IAuthorizationService<DataActions>, DisabledAuthorizationService<DataActions>>();
                }
            }
            else
            {
                services.AddSingleton<IAuthorizationService<DataActions>, DisabledAuthorizationService<DataActions>>();
            }

            services.Add<DicomRequestContextAccessor>()
                .Singleton()
                .AsSelf()
                .AsService<RequestContextAccessor<IDicomRequestContext>>()
                .AsService<IDicomRequestContextAccessor>();

            services.AddSingleton<IClaimsExtractor, PrincipalClaimsExtractor>();
        }

        internal string[] GetValidAudiences()
        {
            if (_securityConfiguration.Authentication.Audiences != null)
            {
                return _securityConfiguration.Authentication.Audiences.ToArray();
            }

            if (!string.IsNullOrWhiteSpace(_securityConfiguration.Authentication.Audience))
            {
                return new[]
                {
                    _securityConfiguration.Authentication.Audience,
                };
            }

            return null;
        }
    }
}
