// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.Dicom.Api.Modules;

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

            string[] prodValidAudiences = new[]
            {
                "https://dicom.healthcareapis.azure.com/",
                "https://dicom.healthcareapis.azure.com"
            };
            string prodChallengeAudience = prodValidAudiences.FirstOrDefault();
            string prodAuthority = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/";
            Dictionary<string, string> audienceToSchemeMapper = new Dictionary<string, string>
            {
                { "https://dicom.healthcareapis.azure.com", "idp4" },
                { "https://dicom.healthcareapis.azure-test.net", "default" },
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer("default", options =>
            {
                options.Authority = _securityConfiguration.Authentication.Authority;
                options.RequireHttpsMetadata = true;
                options.Challenge = $"Bearer authorization_uri=\"{_securityConfiguration.Authentication.Authority}\", resource_id=\"{challengeAudience}\", realm=\"{challengeAudience}\"";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudiences = validAudiences,
                };
            })
            .AddJwtBearer("idp4", options =>
            {
                options.Authority = prodAuthority;
                options.RequireHttpsMetadata = true;
                options.Challenge = $"Bearer authorization_uri=\"{prodAuthority}\", resource_id=\"{prodChallengeAudience}\", realm=\"{prodChallengeAudience}\"";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudiences = prodValidAudiences,
                };
            })
            .AddPolicyScheme(
                JwtBearerDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        // Find the first authentication header with a JWT Bearer token whose issuer
                        // contains one of the scheme names and return the found scheme name.
                        StringValues headers = default(StringValues);

                        if (!StringValues.IsNullOrEmpty(context.Request.Headers.Authorization))
                        {
                            Console.WriteLine("!!!!!!Found Authorization");
                            headers = context.Request.Headers.Authorization;
                        }

                        if (!StringValues.IsNullOrEmpty(context.Request.Headers.WWWAuthenticate))
                        {
                            Console.WriteLine("!!!!!!Found WWWAuthenticate");
                        }

                        if (!StringValues.IsNullOrEmpty(context.Request.Headers.ProxyAuthorization))
                        {
                            Console.WriteLine("!!!!!!Found ProxyAuthorization");
                        }

                        foreach (var header in context.Request.Headers)
                        {
                            Console.WriteLine($"Header: {header.Key}, value {header.Value}");
                        }

                        if (StringValues.IsNullOrEmpty(headers))
                        {
                            // No authentication headers - how do we handle this error?
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            Console.WriteLine("!!!!!!Found no auth headers");
                            return "default";
                        }

                        foreach (var header in headers)
                        {
                            var encodedToken = header.Substring(JwtBearerDefaults.AuthenticationScheme.Length + 1);
                            var jwtHandler = new JwtSecurityTokenHandler();
                            var decodedToken = jwtHandler.ReadJwtToken(encodedToken);
                            var audiences = decodedToken?.Audiences;
                            foreach (var audienceToScheme in audienceToSchemeMapper)
                            {
                                if (audiences != null && audiences.Any() && audiences.Any(a => a.Contains(audienceToScheme.Key, System.StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Found the scheme.
                                    return audienceToScheme.Value;
                                }
                            }
                        }

                        // Handle error.
                        Console.WriteLine("!!!!!!Did not match audicences");
                        return "default";
                    };
                }
            );

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
