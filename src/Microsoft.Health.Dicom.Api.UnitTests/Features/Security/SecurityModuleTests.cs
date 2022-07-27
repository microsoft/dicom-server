// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Modules;
using Microsoft.Health.Dicom.Core.Configs;
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
                AlternativeAuthenticationSchemes = new List<AuthenticationConfiguration>
                {
                    new AuthenticationConfiguration
                    {
                        Audience = "initialAudience",
                    }
                }
            },
        };

        Assert.Equal(new[] { "initialAudience" }, SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.AlternativeAuthenticationSchemes.First()));
    }

    [Fact]
    public void GivenASecurityConfigurationWithAudienceAndAudiences_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                AlternativeAuthenticationSchemes = new List<AuthenticationConfiguration>
                {
                    new AuthenticationConfiguration
                    {
                        Audience = "initialAudience",
                        Audiences = new[] { "audience1", "audience2" }
                    }
                }
            },
        };

        Assert.Equal(new[] { "audience1", "audience2" }, SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.AlternativeAuthenticationSchemes.First()));
    }

    [Fact]
    public void GivenASecurityConfigurationWithAudiences_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                AlternativeAuthenticationSchemes = new List<AuthenticationConfiguration>
                {
                    new AuthenticationConfiguration
                    {
                        Audiences = new[] { "audience1", "audience2" }
                    }
                }
            },
        };


        Assert.Equal(new[] { "audience1", "audience2" }, SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.AlternativeAuthenticationSchemes.First()));
    }

    [Fact]
    public void GivenASecurityConfigurationWithNoAudienceSpecified_WhenGettingValidAudiences_ThenNullShouldBeReturned()
    {
        var dicomServerConfiguration = new DicomServerConfiguration
        {
            Security =
            {
                AlternativeAuthenticationSchemes = new List<AuthenticationConfiguration>
                {
                    new AuthenticationConfiguration()
                }
            },
        };

        Assert.Null(SecurityModule.GetValidAudiences(dicomServerConfiguration.Security.AlternativeAuthenticationSchemes.First()));
    }
}
