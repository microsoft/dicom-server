// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DeleteTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public DeleteTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Theory]
        [InlineData("studies")]
        [InlineData("studies/invalidStudyId")]
        [InlineData("studies/invalidStudyId/series")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances/invalidInstanceId")]
        public async Task GivenInvalidUID_WhenDeleting_TheServerShouldReturnNotFound(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenNonEmptyBody_WhenDeletingStudy_TheServerShouldReturnBadRequest()
        {
            // Create and upload file
            var studyInstanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID);
            await StoreFile(dicomFile);

            // Send the delete request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"studies/{studyInstanceUID}");
            request.Content = new StringContent("Some Body Content");

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenNonEmptyBody_WhenDeletingSeries_TheServerShouldReturnBadRequest()
        {
            // Create and upload file
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesUID = Guid.NewGuid().ToString();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID);
            await StoreFile(dicomFile);

            // Send the delete request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"studies/{studyInstanceUID}/series/{seriesUID}");
            request.Content = new StringContent("Some Body Content");

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenNonEmptyBody_WhenDeletingInstance_TheServerShouldReturnBadRequest()
        {
            // Create and upload file
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesUID = Guid.NewGuid().ToString();
            var instanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID, sopInstanceUID: instanceUID);
            await StoreFile(dicomFile);

            // Send the delete request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"studies/{studyInstanceUID}/series/{seriesUID}/instances/{instanceUID}");
            request.Content = new StringContent("Some Body Content");

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenValidStudyId_WhenDeletingStudy_TheServerShouldReturnOK()
        {
            // Add 10 series with 10 instances each to a single study
            var studyInstanceUID = Guid.NewGuid().ToString();
            for (int i = 0; i < 10; i++)
            {
                var seriesUID = Guid.NewGuid().ToString();
                for (int j = 0; j < 10; j++)
                {
                    DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID);
                    await StoreFile(dicomFile);
                }
            }

            // Send the delete request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"studies/{studyInstanceUID}");

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenValidSeriesId_WhenDeletingSeries_TheServerShouldReturnOK()
        {
            // Store series with 10 instances
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesUID = Guid.NewGuid().ToString();
            for (int i = 0; i < 10; i++)
            {
                DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID);
                await StoreFile(dicomFile);
            }

            // Send the delete request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"studies/{studyInstanceUID}/series/{seriesUID}");

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenValidInstanceId_WhenDeletingInstance_TheServerShouldReturnOK()
        {
            // Create and upload file
            var studyInstanceUID = Guid.NewGuid().ToString();
            var seriesUID = Guid.NewGuid().ToString();
            var instanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesUID, sopInstanceUID: instanceUID);
            await StoreFile(dicomFile);

            // Send the delete request
            var request = new HttpRequestMessage(HttpMethod.Delete, $"studies/{studyInstanceUID}/series/{seriesUID}/instances/{instanceUID}");

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private async Task StoreFile(DicomFile file)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            using (var stream = new MemoryStream())
            {
                await file.SaveAsync(stream);

                var validByteContent = new ByteArrayContent(stream.ToArray());
                validByteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
                multiContent.Add(validByteContent);
            }

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
        }
    }
}
