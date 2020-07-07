// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class AuthenticationTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly HttpIntegrationTestFixture<Startup> _fixture;

        public AuthenticationTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GivenDicomRequest_WithNoAuthenticationToken_ReturnUnauthorized()
        {
            if (AuthenticationSettings.SecurityEnabled)
            {
                IDicomWebClient client = _fixture.GetDicomWebClient(TestApplications.InvalidClient);
                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                    () => client.QueryAsync("/studies"));
                Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
            }
        }
    }
}
