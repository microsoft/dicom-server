// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Api.Configs;
using Microsoft.Health.Dicom.Api.Modules;
using Microsoft.Health.Dicom.Core.Configs;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Features.Security
{
    public class SecurityModuleTests
    {
        [Theory]
        [InlineData("https://example.com", "https://example.com/")]
        [InlineData("https://example.com/", "https://example.com")]
        [InlineData("", "/")]
        [InlineData("/", "")]
        public void GivenASecurityConfiguration_WhenGettingTheSecondaryAudience_ThenCorrectAudienceShouldBeReturned(string initialAudience, string expectedAudience)
        {
            var dicomServerConfiguration = new DicomServerConfiguration
            {
                Security =
                {
                    Authentication = new AuthenticationConfiguration
                    {
                        Audience = initialAudience,
                    },
                },
            };

            var securityModule = new SecurityModule(dicomServerConfiguration);

            Assert.Equal(expectedAudience, securityModule.GenerateSecondaryAudience());
        }
    }
}
