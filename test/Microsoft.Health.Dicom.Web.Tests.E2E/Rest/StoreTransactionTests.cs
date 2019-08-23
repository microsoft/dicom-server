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
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Resources.Store;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Clients;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class StoreTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        public StoreTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            Client = new DicomWebClient(fixture.HttpClient);
        }

        protected DicomWebClient Client { get; set; }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnConflict(DicomWebClient.DicomMediaType dicomMediaType)
        {
            using (var stream = new MemoryStream())
            {
                HttpResult<DicomDataset> response = await Client.PostAsync(new[] { stream }, null, dicomMediaType);
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest(DicomWebClient.DicomMediaType dicomMediaType)
        {
            using (var stream = new MemoryStream())
            {
                HttpResult<DicomDataset> response = await Client.PostAsync(new[] { stream }, studyInstanceUID: new string('b', 65), dicomMediaType);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType(DicomWebClient.DicomMediaType dicomMediaType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            switch (dicomMediaType)
            {
                case DicomWebClient.DicomMediaType.Json:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);
                    break;
                case DicomWebClient.DicomMediaType.Xml:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomXml.MediaType);
                    break;
            }

            request.Content = new ByteArrayContent(new byte[] { 1, 2, 3 });

            using (HttpResponseMessage response = await Client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent(DicomWebClient.DicomMediaType dicomMediaType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            switch (dicomMediaType)
            {
                case DicomWebClient.DicomMediaType.Json:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);
                    break;
                case DicomWebClient.DicomMediaType.Xml:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomXml.MediaType);
                    break;
            }

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenAMultipartRequestWithEmptyContent_WhenStoring_TheServerShouldReturnConflict(DicomWebClient.DicomMediaType dicomMediaType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            switch (dicomMediaType)
            {
                case DicomWebClient.DicomMediaType.Json:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);
                    break;
                case DicomWebClient.DicomMediaType.Xml:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomXml.MediaType);
                    break;
            }

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            request.Content = multiContent;

            HttpResult<DicomDataset> response = await Client.PostMultipartContentAsync(multiContent, "studies");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenAMultipartRequestWithAnInvalidMultipartSection_WhenStoring_TheServerShouldReturnAccepted(DicomWebClient.DicomMediaType dicomMediaType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            switch (dicomMediaType)
            {
                case DicomWebClient.DicomMediaType.Json:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);
                    break;
                case DicomWebClient.DicomMediaType.Xml:
                    request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomXml.MediaType);
                    break;
            }

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            DicomFile validFile = Samples.CreateRandomDicomFile();

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
            ValidateSuccessSequence(response.Value.GetSequence(DicomTag.ReferencedSOPSequence), validFile.Dataset);
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenAllDifferentStudyInstanceUIDs_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnConflict(DicomWebClient.DicomMediaType dicomMediaType)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

            var studyInstanceUID = Guid.NewGuid().ToString();
            HttpResult<DicomDataset> response = await Client.PostAsync(
                new[] { dicomFile1, dicomFile2 }, studyInstanceUID, dicomMediaType);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.NotNull(response.Value);
            Assert.True(response.Value.Count() == 2);

            Assert.EndsWith($"studies/{studyInstanceUID}", response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));

            ValidateFailureSequence(
                response.Value.GetSequence(DicomTag.FailedSOPSequence),
                StoreFailureCodes.MismatchStudyInstanceUIDFailureCode,
                dicomFile1.Dataset,
                dicomFile2.Dataset);
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenOneDifferentStudyInstanceUID_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnAccepted(DicomWebClient.DicomMediaType dicomMediaType)
        {
            var studyInstanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID: studyInstanceUID);
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

            HttpResult<DicomDataset> response = await Client.PostAsync(
                new[] { dicomFile1, dicomFile2 }, studyInstanceUID, dicomMediaType);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.NotNull(response.Value);
            Assert.True(response.Value.Count() == 3);

            Assert.EndsWith($"studies/{studyInstanceUID}", response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));

            ValidateSuccessSequence(response.Value.GetSequence(DicomTag.ReferencedSOPSequence), dicomFile1.Dataset);
            ValidateFailureSequence(
                response.Value.GetSequence(DicomTag.FailedSOPSequence),
                StoreFailureCodes.MismatchStudyInstanceUIDFailureCode,
                dicomFile2.Dataset);
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenDatasetWithDuplicateIdentifiers_WhenStoring_TheServerShouldReturnConflict(DicomWebClient.DicomMediaType dicomMediaType)
        {
            var studyInstanceUID = Guid.NewGuid().ToString();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, studyInstanceUID);
            HttpResult<DicomDataset> response = await Client.PostAsync(new[] { dicomFile1 }, null, dicomMediaType);
            Assert.False(response.Value.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            ValidateFailureSequence(
                response.Value.GetSequence(DicomTag.FailedSOPSequence),
                StoreFailureCodes.ProcessingFailureCode,
                dicomFile1.Dataset);
        }

        [Theory]
        [InlineData(DicomWebClient.DicomMediaType.Json)]
        [InlineData(DicomWebClient.DicomMediaType.Xml)]
        public async void GivenExistingDataset_WhenStoring_TheServerShouldReturnConflict(DicomWebClient.DicomMediaType dicomMediaType)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
            HttpResult<DicomDataset> response1 = await Client.PostAsync(new[] { dicomFile1 }, null, dicomMediaType);
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            ValidateSuccessSequence(response1.Value.GetSequence(DicomTag.ReferencedSOPSequence), dicomFile1.Dataset);
            Assert.False(response1.Value.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence _));

            HttpResult<DicomDataset> response2 = await Client.PostAsync(new[] { dicomFile1 }, null, dicomMediaType);
            Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
            ValidateFailureSequence(
                response2.Value.GetSequence(DicomTag.FailedSOPSequence),
                StoreFailureCodes.SopInstanceAlredyExistsFailureCode,
                dicomFile1.Dataset);
        }

        private void ValidateFailureSequence(DicomSequence dicomSequence, ushort expectedFailureCode, params DicomDataset[] expectedFailedDatasets)
        {
            Assert.Equal(DicomTag.FailedSOPSequence, dicomSequence.Tag);
            Assert.True(dicomSequence.Count() == expectedFailedDatasets.Length);
            var expectedSopClassUIDs = new HashSet<string>(expectedFailedDatasets.Select(x => x.GetSingleValueOrDefault(DicomTag.SOPClassUID, string.Empty)));
            var expectedSopInstanceUIDs = new HashSet<string>(expectedFailedDatasets.Select(x => x.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty)));

            foreach (DicomDataset dataset in dicomSequence)
            {
                Assert.True(dataset.Count() == 3);
                Assert.Equal(expectedFailureCode, dataset.GetSingleValue<ushort>(DicomTag.FailureReason));
                Assert.Contains(dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID), expectedSopClassUIDs);
                Assert.Contains(dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID), expectedSopInstanceUIDs);
            }
        }

        private void ValidateSuccessSequence(DicomSequence dicomSequence, params DicomDataset[] expectedDatasets)
        {
            Assert.Equal(DicomTag.ReferencedSOPSequence, dicomSequence.Tag);
            Assert.True(dicomSequence.Count() == expectedDatasets.Length);
            var datasetsBySopInstanceUID = expectedDatasets.ToDictionary(x => x.GetSingleValue<string>(DicomTag.SOPInstanceUID), x => x);

            foreach (DicomDataset dataset in dicomSequence)
            {
                Assert.True(dataset.Count() == 3);
                var referencedSopInstanceUID = dataset.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.True(datasetsBySopInstanceUID.ContainsKey(referencedSopInstanceUID));
                DicomDataset referenceDataset = datasetsBySopInstanceUID[referencedSopInstanceUID];
                var dicomInstance = DicomInstance.Create(referenceDataset);
                Assert.Equal(referenceDataset.GetSingleValue<string>(DicomTag.SOPClassUID), dataset.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID));
                Assert.EndsWith(
                    $"studies/{dicomInstance.StudyInstanceUID}/series/{dicomInstance.SeriesInstanceUID}/instances/{dicomInstance.SopInstanceUID}",
                    dataset.GetSingleValue<string>(DicomTag.RetrieveURL));
            }
        }
    }
}
