// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Features.Security;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.Dicom.Api.Modules;

public class SecurityModule : IStartupModule
{
    private readonly SecurityConfiguration _securityConfiguration;
    private readonly Dictionary<string, string> _tenantToSchemeMap;
    private readonly IJwtSecurityTokenParser _jwtSecurityTokenParser;
    internal string _defaultScheme;

    public SecurityModule(DicomServerConfiguration dicomServerConfiguration, IJwtSecurityTokenParser jwtSecurityTokenParser)
    {
        EnsureArg.IsNotNull(dicomServerConfiguration, nameof(dicomServerConfiguration));
        _securityConfiguration = dicomServerConfiguration.Security;
        _jwtSecurityTokenParser = jwtSecurityTokenParser;
        _tenantToSchemeMap = new Dictionary<string, string>();
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
                        return FindAppropriateScheme(context.Request.Headers);
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

    internal string FindAppropriateScheme(IHeaderDictionary requestHeaders)
    {
        // Find the first authentication header with a JWT Bearer token whose issuer
        // contains one of the scheme names and return the found scheme name.
        StringValues authHeaders = default(StringValues);

        if (!StringValues.IsNullOrEmpty(requestHeaders.Authorization))
        {
            authHeaders = requestHeaders.Authorization;
        }
        else if (!StringValues.IsNullOrEmpty(requestHeaders.WWWAuthenticate))
        {
            authHeaders = requestHeaders.WWWAuthenticate;
        }
        else
        {
            return _defaultScheme;
        }

        foreach (var authHeader in authHeaders)
        {
            var encodedToken = authHeader.Substring(JwtBearerDefaults.AuthenticationScheme.Length + 1);
            var issuer = _jwtSecurityTokenParser.GetIssuer(encodedToken);

            if (string.IsNullOrEmpty(issuer))
            {
                return _defaultScheme;
            }

            var tenantId = ExtractTenantId(issuer);

            foreach (var tenantToScheme in _tenantToSchemeMap)
            {
                if (tenantId.Equals(tenantToScheme.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Found the scheme.
                    return tenantToScheme.Value;
                }
            }
        }

        return _defaultScheme;
    }

    internal void AddAuthenticationSchemes(AuthenticationBuilder authenticationBuilder)
    {
        // add default scheme
        AddScheme(authenticationBuilder, _securityConfiguration.Authentication);
        _defaultScheme = _securityConfiguration.Authentication.Authority;

        // add internal authentication scheme
        AddScheme(authenticationBuilder, _securityConfiguration.InternalAuthenticationScheme);
    }

    private void AddScheme(AuthenticationBuilder authenticationBuilder, AuthenticationConfiguration scheme)
    {
        string[] validAudiences = GetValidAudiences(scheme);
        string challengeAudience = validAudiences?.FirstOrDefault();
        _tenantToSchemeMap.Add(ExtractTenantId(scheme.Authority), scheme.Authority);

        authenticationBuilder.AddJwtBearer(scheme.Authority, options =>
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

    private static string ExtractTenantId(string authority)
    {
        Match guidMatchForTenantId = Regex.Match(authority, @"[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}");
        if (!guidMatchForTenantId.Success)
        {
            throw new ArgumentException($"Could not parse TenantId from {authority}.");
        }

        return guidMatchForTenantId.Value;
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
