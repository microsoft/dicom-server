// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class AuthorizationTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly HttpIntegrationTestFixture<Startup> _fixture;

        public AuthorizationTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GivenPostDicomRequest_WithAReadOnlyToken_ReturnUnauthorized()
        {
            if (AuthenticationSettings.SecurityEnabled)
            {
                DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: 1);
                var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();

                IDicomWebClient client = _fixture.GetDicomWebClient(TestApplications.GlobalAdminServicePrincipal, TestUsers.User1);
                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                    () => client.StoreAsync(new[] { dicomFile }, dicomInstance.StudyInstanceUid));
                Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
            }
        }
    }
}
