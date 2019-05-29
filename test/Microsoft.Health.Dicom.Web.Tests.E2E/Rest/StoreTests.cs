// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class StoreTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public StoreTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Fact]
        public async void GivenRandomContent_WhenStoring_TheServerShouldReturnOK()
        {
            var stream = new MemoryStream();
            HttpStatusCode response = await Client.PostAsync(new[] { stream });

            Assert.Equal(HttpStatusCode.OK, response);
        }
    }
}
