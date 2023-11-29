// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;
using Partition = Microsoft.Health.Dicom.Core.Features.Partitioning.Partition;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class AuthorizationTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
{
    private readonly HttpIntegrationTestFixture<Startup> _fixture;
    private readonly IDicomWebClient _clientV2WithReader;

    public AuthorizationTests(HttpIntegrationTestFixture<Startup> fixture)
    {
        _fixture = fixture;
        _clientV2WithReader = fixture.GetDicomWebClient(TestApplications.GlobalReaderServicePrincipal);
    }

    [Fact]
    public async Task GivenPostDicomRequest_WithAReadOnlyToken_ReturnUnauthorized()
    {
        if (AuthenticationSettings.SecurityEnabled)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: 1);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier(Partition.Default);

            IDicomWebClient client = _fixture.GetDicomWebClient(TestApplications.GlobalAdminServicePrincipal, TestUsers.User1);
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => client.StoreAsync(new[] { dicomFile }, dicomInstance.StudyInstanceUid));
            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenSearchRequest_WithValidParamsAndNoMatchingResult_ReturnNoContent()
    {
        using DicomWebAsyncEnumerableResponse<DicomDataset> response = await _clientV2WithReader.QueryStudyAsync("StudyDate=20200101");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenAValidQueryString_WhenRetrievingChangeFeedLatest_ThenReturnsSuccessfulStatusCode()
    {
        using DicomWebResponse<ChangeFeedEntry> response = await _clientV2WithReader.GetChangeFeedLatest();
        Assert.True(response.IsSuccessStatusCode);
    }
}
