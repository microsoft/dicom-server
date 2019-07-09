// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Net.Http.Headers;
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
        public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnOK()
        {
            using (var stream = new MemoryStream(new byte[5]))
            {
                HttpStatusCode response = await Client.PostAsync(new[] { stream });
                Assert.Equal(HttpStatusCode.OK, response);
            }
        }

        [Fact]
        public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
        {
            using (var stream = new MemoryStream())
            {
                HttpStatusCode response = await Client.PostAsync(new[] { stream }, studyInstanceUID: new string('b', 65));
                Assert.Equal(HttpStatusCode.BadRequest, response);
            }
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/dicom")]
        public async void GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable(string acceptHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, acceptHeader);

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);
            request.Content = new ByteArrayContent(new byte[] { 1, 2, 3 });

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }
    }
}
