// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Dicom;
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
        public async void GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, "application/xml");

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

        [Fact]
        public async void GivenAMultipartRequestWithInvalidHeadersPerPart_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicomJson;
            multiContent.Add(byteContent);

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async void GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async void GivenAMultipartRequestWithEmptyContent_WhenStoring_TheServerShouldReturnConflict()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async void GivenAMultipartRequestWithAnInvalidMultipartSection_WhenStoring_TheServerShouldReturnAccepted()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            DicomFile validFile = DicomSamples.GetSampleCTSeries().First();

            using (var stream = new MemoryStream())
            {
                await validFile.SaveAsync(stream);

                var validByteContent = new ByteArrayContent(stream.ToArray());
                validByteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
                multiContent.Add(validByteContent);
            }

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            ValidateResponseDataset(response.Value, new[] { validFile }, 0);
        }

        [Fact]
        public async void GivenAValidSeriesWithOneInvalidFile_WhenStoring_TheServerShouldReturnAccepted()
        {
            IList<DicomFile> content = DicomSamples.GetSampleCTSeries().ToList();

            DicomFile invalidFile = content.Last();
            invalidFile.Dataset.Remove(DicomTag.SeriesInstanceUID);

            HttpResult<DicomDataset> response = await Client.PostAsync(content);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.NotNull(response.Value);

            ValidateResponseDataset(response.Value, expectedSucceeded: content.SkipLast(1), expectedFailedCount: 1);
            ValidateFailedResponseSequence(response.Value, expectedFailed: new[] { invalidFile });
        }

        [Fact]
        public async void GivenAValidSeriesWithOneDifferentStudyId_WhenStoringWithStudyId_TheServerShouldReturnAccepted()
        {
            IList<DicomFile> content = DicomSamples.GetSampleCTSeries().ToList();

            DicomFile invalidFile = content.Last();
            string currentStudyInstanceUID = invalidFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            invalidFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, Guid.NewGuid().ToString());

            HttpResult<DicomDataset> response = await Client.PostAsync(content, studyInstanceUID: currentStudyInstanceUID);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.NotNull(response.Value);

            ValidateResponseDataset(response.Value, expectedSucceeded: content.SkipLast(1), expectedFailedCount: 1);
            ValidateFailedResponseSequence(response.Value, expectedFailed: new[] { invalidFile });
        }

        [Fact]
        public async void GivenValidCTSeries_WhenStoring_TheServerShouldReturnOK()
        {
            IEnumerable<DicomFile> content = DicomSamples.GetSampleCTSeries();
            string studyInstanceUID = content.First().Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            HttpResult<DicomDataset> response = await Client.PostAsync(content, studyInstanceUID);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Value);

            ValidateResponseDataset(response.Value, expectedSucceeded: content);
            Assert.EndsWith(
                $"/studies/{studyInstanceUID}",
                response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));
        }

        [Fact]
        public async void GivenValidCTSeries_WhenStoringWithInvalidStudyInstanceUID_TheServerShouldReturnConflict()
        {
            IEnumerable<DicomFile> content = DicomSamples.GetSampleCTSeries();
            HttpResult<DicomDataset> response = await Client.PostAsync(content, studyInstanceUID: Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Value);

            ValidateFailedResponseSequence(response.Value, expectedFailed: content);
        }

        private void ValidateResponseDataset(DicomDataset responseDataset, IEnumerable<DicomFile> expectedSucceeded = null, int expectedFailedCount = 0)
        {
            responseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence insertedSequence);
            Assert.Equal(expectedSucceeded?.Count() ?? 0, insertedSequence?.Count() ?? 0);

            foreach (DicomDataset dataset in insertedSequence)
            {
                var referencedSopClassUID = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                var referencedSopInstanceUID = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                var retrieveUrl = dataset.GetSingleValue<string>(DicomTag.RetrieveURL);

                DicomFile referencedDicomFile = expectedSucceeded.First(x =>
                        x.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) == referencedSopInstanceUID &&
                        x.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID) == referencedSopClassUID);

                var referencedStudyInstanceUID = referencedDicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                var referencedSeriesInstanceUID = referencedDicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                Assert.EndsWith($"/studies/{referencedStudyInstanceUID}/series/{referencedSeriesInstanceUID}/instances/{referencedSopInstanceUID}", retrieveUrl);
            }

            responseDataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence failedSequence);
            Assert.Equal(expectedFailedCount, failedSequence?.Count() ?? 0);
        }

        private void ValidateFailedResponseSequence(DicomDataset responseDataset, IEnumerable<DicomFile> expectedFailed)
        {
            responseDataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence failedSequence);
            Assert.Equal(expectedFailed?.Count() ?? 0, failedSequence?.Count() ?? 0);

            foreach (DicomDataset dataset in failedSequence)
            {
                var referencedSopClassUID = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                var referencedSopInstanceUID = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);

                DicomFile referencedDicomFile = expectedFailed.First(x =>
                        x.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID) == referencedSopInstanceUID &&
                        x.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID) == referencedSopClassUID);

                Assert.Equal(272, dataset.GetSingleValue<ushort>(DicomTag.FailureReason));
            }
        }
    }
}
