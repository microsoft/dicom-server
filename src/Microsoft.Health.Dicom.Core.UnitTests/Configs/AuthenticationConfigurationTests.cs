// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Configs;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Configs
{
    public class AuthenticationConfigurationTests
    {
        [Fact]
        public void GivenASecurityConfigurationWithAudience_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
        {
            var config = new AuthenticationConfiguration { Audience = "initialAudience" };
            Assert.Equal(new[] { "initialAudience" }, config.GetValidAudiences());
        }

        [Fact]
        public void GivenASecurityConfigurationWithAudienceAndAudiences_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
        {
            var config = new AuthenticationConfiguration
            {
                Audience = "initialAudience",
                Audiences = new[] { "audience1", "audience2" },
            };

            Assert.Equal(new[] { "audience1", "audience2" }, config.GetValidAudiences());
        }

        [Fact]
        public void GivenASecurityConfigurationWithAudiences_WhenGettingValidAudiences_ThenCorrectAudienceShouldBeReturned()
        {
            var config = new AuthenticationConfiguration
            {
                Audiences = new[] { "audience1", "audience2" },
            };

            Assert.Equal(new[] { "audience1", "audience2" }, config.GetValidAudiences());
        }

        [Fact]
        public void GivenASecurityConfigurationWithNoAudienceSpecified_WhenGettingValidAudiences_ThenNullShouldBeReturned()
        {
            var config = new AuthenticationConfiguration();
            Assert.Null(config.GetValidAudiences());
        }
    }
}
