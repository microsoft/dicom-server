// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class DeleteTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly DicomWebClient _client;

        public DeleteTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
        }

        [Theory]
        [InlineData("studies")]
        [InlineData("studies/")]
        [InlineData("studies/invalidStudyId")]
        [InlineData("studies/invalidStudyId/series")]
        [InlineData("studies/invalidStudyId/series/")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances/")]
        [InlineData("studies/invalidStudyId/series/invalidSeriesId/instances/invalidInstanceId")]
        [InlineData("studies//series/invalidSeriesId")]
        [InlineData("studies/invalidStudyId/series//instances/invalidInstanceId")]
        public async Task GivenInvalidUID_WhenDeleting_TheServerShouldReturnNotFound(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("studies/&^%")]
        [InlineData("studies/123/series/&^%")]
        [InlineData("studies/123/series/456/instances/&^%")]
        public async Task GivenBadUIDFormats_WhenDeleting_TheServerShouldReturnBadRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenValidStudyId_WhenDeletingStudy_TheServerShouldReturnOK()
        {
            // Add 10 series with 10 instances each to a single study
            const int numberOfStudies = 2;
            var studyInstanceUID = DicomUID.Generate().UID;
            for (int i = 0; i < numberOfStudies; i++)
            {
                var files = new DicomFile[10];
                var seriesInstanceUID = DicomUID.Generate().UID;

                for (int j = 0; j < 10; j++)
                {
                    files[j] = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesInstanceUID);
                }

                await _client.PostAsync(files);
            }

            // Send the delete request
            HttpStatusCode result = await _client.DeleteAsync(studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, result);

            // Validate not found
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.GetStudyAsync(studyInstanceUID);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async void GivenValidSeriesId_WhenDeletingSeries_TheServerShouldReturnOK()
        {
            // Store series with 10 instances
            var studyInstanceUID = DicomUID.Generate().UID;
            var seriesInstanceUID = DicomUID.Generate().UID;
            var files = new DicomFile[10];
            for (int i = 0; i < 10; i++)
            {
                files[i] = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesInstanceUID);
            }

            await _client.PostAsync(files);

            // Send the delete request
            HttpStatusCode result = await _client.DeleteAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(HttpStatusCode.OK, result);

            // Validate not found
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.GetSeriesAsync(studyInstanceUID, seriesInstanceUID);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async void GivenValidInstanceId_WhenDeletingInstance_TheServerShouldReturnOK()
        {
            // Create and upload file
            var studyInstanceUID = DicomUID.Generate().UID;
            var seriesInstanceUID = DicomUID.Generate().UID;
            var sopInstanceUID = DicomUID.Generate().UID;
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID, seriesInstanceUID: seriesInstanceUID, sopInstanceUID: sopInstanceUID);
            await _client.PostAsync(new[] { dicomFile });

            // Send the delete request
            HttpStatusCode result = await _client.DeleteAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            Assert.Equal(HttpStatusCode.OK, result);

            // Validate not found
            HttpResult<IReadOnlyList<DicomFile>> response = await _client.GetInstanceAsync(studyInstanceUID, seriesInstanceUID, sopInstanceUID);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
