// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public class HealthCheckTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(HttpIntegrationTestFixture<Startup> fixture)
        => _client = EnsureArg.IsNotNull(fixture, nameof(fixture)).GetDicomWebClient().HttpClient;

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenDicomService_WhenCheckingHealth_ThenReturnHealthy()
    {
        HttpResponseMessage response = await _client.GetAsync("/health/check");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
