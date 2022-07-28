// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Features.Security;
using Microsoft.Health.Dicom.Api.Modules;
using Microsoft.Health.Dicom.Core.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Security;

public class SecurityModuleTests
{
    [Fact]
    public void GivenASecurityConfigurationWithAudience_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                Authentication = new AuthenticationConfiguration
                {
                    Audience = "initialAudience",
                }
            },
        };

        Assert.Equal(new[] { "initialAudience" }, SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.Authentication));
    }

    [Fact]
    public void GivenASecurityConfigurationWithAudienceAndAudiences_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                Authentication = new AuthenticationConfiguration
                {
                    Audience = "initialAudience",
                    Audiences = new[] { "audience1", "audience2" },
                },
            },
        };

        Assert.Equal(new[] { "audience1", "audience2" }, SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.Authentication));
    }

    [Fact]
    public void GivenASecurityConfigurationWithAudiences_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                Authentication = new AuthenticationConfiguration
                {
                    Audiences = new[] { "audience1", "audience2" },
                },
            },
        };

        Assert.Equal(new[] { "audience1", "audience2" }, SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.Authentication));
    }

    [Fact]
    public void GivenASecurityConfigurationWithNoAudienceSpecified_WhenGettingValidAudiences_ThenNullShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                Authentication = new AuthenticationConfiguration(),
            },
        };

        Assert.Null(SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.Authentication));
    }

    [Fact]
    public void GivenRequestHeaders_WithNoAuthHeader_ReturnDefaultSchema()
    {
        DicomServerConfiguration config = new DicomServerConfiguration();
        var securityModule = new SecurityModule(config, new JwtSecurityTokenParser());
        securityModule._defaultScheme = "defaultSchemeName";

        IHeaderDictionary headers = Substitute.For<IHeaderDictionary>();
        headers.Authorization = default(StringValues);
        headers.WWWAuthenticate = default(StringValues);

        var scheme = securityModule.FindAppropriateScheme(headers);

        Assert.Equal(securityModule._defaultScheme, scheme);
    }

    [Fact]
    public void GivenRequestHeaders_WithDefaultAuthToken_ReturnDefaultSchema()
    {
        var defaultTenantId = Guid.NewGuid();

        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                Authentication = new AuthenticationConfiguration
                {
                    Audiences = new[] { "audience1", "audience2" },
                    Authority = $"someauth{defaultTenantId}",
                },
                InternalAuthenticationScheme = new AuthenticationConfiguration
                {
                    Audiences = new[] { "audience3", "audience4" },
                    Authority = $"anotherauth{Guid.NewGuid()}",
                }
            },
        };

        var jwtSecurityTokenParser = Substitute.For<IJwtSecurityTokenParser>();
        jwtSecurityTokenParser.GetIssuer(Arg.Any<string>()).Returns($"issuer{defaultTenantId}");

        var securityModule = new SecurityModule(dicomServerConfiguration, jwtSecurityTokenParser);

        // set up schemes
        var serviceCollection = Substitute.For<IServiceCollection>();
        var authenticationBuilder = new AuthenticationBuilder(serviceCollection);
        securityModule.AddAuthenticationSchemes(authenticationBuilder);

        IHeaderDictionary headers = Substitute.For<IHeaderDictionary>();
        headers.Authorization = "Bearer aJwtToken";

        var scheme = securityModule.FindAppropriateScheme(headers);

        Assert.Equal(securityModule._defaultScheme, scheme);
    }

    [Fact]
    public void GivenRequestHeaders_WithInternalAuthToken_ReturnInternalSchema()
    {
        var internalTenantId = Guid.NewGuid();

        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                Authentication = new AuthenticationConfiguration
                {
                    Audiences = new[] { "audience1", "audience2" },
                    Authority = $"someauth{Guid.NewGuid()}",
                },
                InternalAuthenticationScheme = new AuthenticationConfiguration
                {
                    Audiences = new[] { "audience3", "audience4" },
                    Authority = $"anotherauth{internalTenantId}",
                }
            },
        };

        var jwtSecurityTokenParser = Substitute.For<IJwtSecurityTokenParser>();
        jwtSecurityTokenParser.GetIssuer(Arg.Any<string>()).Returns($"issuer{internalTenantId}");

        var securityModule = new SecurityModule(dicomServerConfiguration, jwtSecurityTokenParser);

        // set up schemes
        var serviceCollection = Substitute.For<IServiceCollection>();
        var authenticationBuilder = new AuthenticationBuilder(serviceCollection);
        securityModule.AddAuthenticationSchemes(authenticationBuilder);

        IHeaderDictionary headers = Substitute.For<IHeaderDictionary>();
        headers.Authorization = "Bearer aJwtToken";

        var scheme = securityModule.FindAppropriateScheme(headers);

        Assert.Equal(dicomServerConfiguration.Security.InternalAuthenticationScheme.Authority, scheme);
    }
}
