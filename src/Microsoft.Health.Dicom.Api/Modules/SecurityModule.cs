// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using EnsureThat;
using Microsoft.AspNetCore.Authentication;
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
    private readonly Dictionary<string, string> _audienceToSchemeMapper;
    private string _defaultSchemeName;

    public SecurityModule(DicomServerConfiguration dicomServerConfiguration)
    {
        EnsureArg.IsNotNull(dicomServerConfiguration, nameof(dicomServerConfiguration));
        _securityConfiguration = dicomServerConfiguration.Security;
        _audienceToSchemeMapper = new Dictionary<string, string>();
    }

    public void Load(IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        // Set the token handler to not do auto inbound mapping. (e.g. "roles" -> "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        if (_securityConfiguration.Enabled)
        {
            AuthenticationBuilder authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            AddAuthenticationSchemes(authenticationBuilder);

            authenticationBuilder.AddPolicyScheme(
                JwtBearerDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        // Find the first authentication header with a JWT Bearer token whose issuer
                        // contains one of the scheme names and return the found scheme name.
                        StringValues authHeaders = default(StringValues);

                        if (!StringValues.IsNullOrEmpty(context.Request.Headers.Authorization))
                        {
                            authHeaders = context.Request.Headers.Authorization;
                        }
                        else if (!StringValues.IsNullOrEmpty(context.Request.Headers.WWWAuthenticate))
                        {
                            authHeaders = context.Request.Headers.WWWAuthenticate;
                        }
                        else
                        {
                            return _defaultSchemeName;
                        }

                        foreach (var authHeader in authHeaders)
                        {
                            var encodedToken = authHeader.Substring(JwtBearerDefaults.AuthenticationScheme.Length + 1);
                            var jwtHandler = new JwtSecurityTokenHandler();
                            var decodedToken = jwtHandler.ReadJwtToken(encodedToken);
                            var audiences = decodedToken?.Audiences;
                            foreach (var audienceToScheme in _audienceToSchemeMapper)
                            {
                                if (audiences != null && audiences.Any() && audiences.Any(a => a.Contains(audienceToScheme.Key, System.StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Found the scheme.
                                    return audienceToScheme.Value;
                                }
                            }
                        }

                        return _defaultSchemeName;
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

    private void AddAuthenticationSchemes(AuthenticationBuilder authenticationBuilder)
    {
        if (string.IsNullOrEmpty(_securityConfiguration.Authentication.Name))
        {
            _securityConfiguration.Authentication.Name = "defaultScheme";
        }

        AddScheme(authenticationBuilder, _securityConfiguration.Authentication);
        _defaultSchemeName = _securityConfiguration.Authentication.Name;

        foreach (var scheme in _securityConfiguration.AlternativeAuthenticationSchemes)
        {
            AddScheme(authenticationBuilder, scheme);
        }
    }

    private void AddScheme(AuthenticationBuilder authenticationBuilder, AuthenticationConfiguration scheme)
    {
        string[] validAudiences = GetValidAudiences(scheme);
        string challengeAudience = validAudiences?.FirstOrDefault();
        AddToAudienceToSchemeMap(validAudiences, scheme.Name);

        authenticationBuilder.AddJwtBearer(scheme.Name, options =>
        {
            options.Authority = scheme.Authority;
            options.RequireHttpsMetadata = true;
            options.Challenge = $"Bearer authorization_uri=\"{scheme.Authority}\", resource_id=\"{challengeAudience}\", realm=\"{challengeAudience}\"";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidAudiences = validAudiences,
            };
        });
    }

    private void AddToAudienceToSchemeMap(string[] audiences, string schemeName)
    {
        foreach (string audience in audiences)
        {
            _audienceToSchemeMapper.Add(audience, schemeName);
        }
    }

    internal static string[] GetValidAudiences(AuthenticationConfiguration authenticationConfiguration)
    {
        if (authenticationConfiguration.Audiences != null)
        {
            return authenticationConfiguration.Audiences.ToArray();
        }

        if (!string.IsNullOrWhiteSpace(authenticationConfiguration.Audience))
        {
            return new[]
            {
                authenticationConfiguration.Audience,
            };
        }

        return null;
    }
}
