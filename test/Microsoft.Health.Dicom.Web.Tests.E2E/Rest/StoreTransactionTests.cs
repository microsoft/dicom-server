// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class StoreTransactionTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private const ushort ValidationFailedFailureCode = 43264;
        private const ushort SopInstanceAlreadyExistsFailureCode = 45070;
        private const ushort MismatchStudyInstanceUidFailureCode = 43265;

        private readonly DicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public StoreTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnConflict()
        {
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(() => _client.StoreAsync(new[] { stream }));
                Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            }
        }

        [Fact]
        public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
        {
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { stream }, studyInstanceUid: new string('b', 65)));
                Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            }
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/dicom")]
        public async void GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable(string acceptHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, acceptHeader);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("form");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));
            request.Content = multiContent;

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            }
        }

        [Fact]
        public async void GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            request.Content = multiContent;

            DicomWebResponse response = await _client.PostMultipartContentAsync(multiContent, "studies");
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

            DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(
                () => _client.PostMultipartContentAsync(multiContent, "studies"));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
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

            string studyInstanceUID = TestUidGenerator.Generate();
            try
            {
                DicomFile validFile = Samples.CreateRandomDicomFile(studyInstanceUID);

                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    await validFile.SaveAsync(stream);

                    var validByteContent = new ByteArrayContent(stream.ToArray());
                    validByteContent.Headers.ContentType = DicomWebClient.MediaTypeApplicationDicom;
                    multiContent.Add(validByteContent);
                }

                request.Content = multiContent;

                DicomWebResponse<DicomDataset> response = await _client.PostMultipartContentAsync(multiContent, "studies");
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                ValidationHelpers.ValidateReferencedSopSequence(
                    response.Value,
                    ConvertToReferencedSopSequenceEntry(validFile.Dataset));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async void GivenAMultipartRequestWithTypeParameterAndFirstSectionWithoutContentType_WhenStoring_TheServerShouldReturnOK()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "studies");
            request.Headers.Add(HeaderNames.Accept, DicomWebClient.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebClient.MediaTypeApplicationDicom.MediaType}\""));

            string studyInstanceUID = TestUidGenerator.Generate();

            try
            {
                DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUID);

                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    await dicomFile.SaveAsync(stream);

                    var byteContent = new ByteArrayContent(stream.ToArray());
                    multiContent.Add(byteContent);
                }

                request.Content = multiContent;

                DicomWebResponse<DicomDataset> response = await _client.PostMultipartContentAsync(multiContent, "studies");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                ValidationHelpers.ValidateReferencedSopSequence(
                    response.Value,
                    ConvertToReferencedSopSequenceEntry(dicomFile.Dataset));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async void GivenAllDifferentStudyInstanceUIDs_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnConflict()
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

            var studyInstanceUID = TestUidGenerator.Generate();

            DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(() => _client.StoreAsync(
                new[] { dicomFile1, dicomFile2 }, studyInstanceUID));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.NotNull(exception.Value);
            Assert.True(exception.Value.Count() == 1);

            ValidationHelpers.ValidateFailedSopSequence(
                exception.Value,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, MismatchStudyInstanceUidFailureCode),
                ConvertToFailedSopSequenceEntry(dicomFile2.Dataset, MismatchStudyInstanceUidFailureCode));
        }

        [Fact]
        public async void GivenOneDifferentStudyInstanceUID_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnAccepted()
        {
            var studyInstanceUID1 = TestUidGenerator.Generate();
            var studyInstanceUID2 = TestUidGenerator.Generate();

            try
            {
                DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID1);
                DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID2);

                DicomWebResponse<DicomDataset> response = await _client.StoreAsync(
                    new[] { dicomFile1, dicomFile2 }, studyInstanceUID1);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                Assert.NotNull(response.Value);
                Assert.True(response.Value.Count() == 3);

                Assert.EndsWith($"studies/{studyInstanceUID1}", response.Value.GetSingleValue<string>(DicomTag.RetrieveURL));

                ValidationHelpers.ValidateReferencedSopSequence(
                    response.Value,
                    ConvertToReferencedSopSequenceEntry(dicomFile1.Dataset));

                ValidationHelpers.ValidateFailedSopSequence(
                    response.Value,
                    ConvertToFailedSopSequenceEntry(dicomFile2.Dataset, MismatchStudyInstanceUidFailureCode));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID1);
            }
        }

        [Fact]
        public async void GivenDatasetWithDuplicateIdentifiers_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, studyInstanceUID);

            DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(
                () => _client.StoreAsync(new[] { dicomFile1 }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.False(exception.Value.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

            ValidationHelpers.ValidateFailedSopSequence(
                exception.Value,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationFailedFailureCode));
        }

        [Fact]
        public async void GivenExistingDataset_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            try
            {
                DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);
                DicomWebResponse<DicomDataset> response1 = await _client.StoreAsync(new[] { dicomFile1 });
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

                ValidationHelpers.ValidateReferencedSopSequence(
                    response1.Value,
                    ConvertToReferencedSopSequenceEntry(dicomFile1.Dataset));

                Assert.False(response1.Value.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence _));

                DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(
                    () => _client.StoreAsync(new[] { dicomFile1 }));

                Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

                ValidationHelpers.ValidateFailedSopSequence(
                    exception.Value,
                    ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, SopInstanceAlreadyExistsFailureCode));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async void GivenDatasetWithInvalidVrValue_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithInvalidVr(studyInstanceUID);

            DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(
            () => _client.StoreAsync(new[] { dicomFile1 }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.False(exception.Value.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

            ValidationHelpers.ValidateFailedSopSequence(
                exception.Value,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationFailedFailureCode));
        }

        [Theory]
        [InlineData("abc.123")]
        [InlineData("11|")]
        public async void GivenDatasetWithInvalidUid_WhenStoring_TheServerShouldReturnConflict(string studyInstanceUID)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = false;
#pragma warning restore CS0618 // Type or member is obsolete

            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);

#pragma warning disable CS0618 // Type or member is obsolete
            DicomValidation.AutoValidation = true;
#pragma warning restore CS0618 // Type or member is obsolete

            DicomWebException<DicomDataset> exception = await Assert.ThrowsAsync<DicomWebException<DicomDataset>>(
           () => _client.StoreAsync(new[] { dicomFile1 }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.False(exception.Value.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

            ValidationHelpers.ValidateFailedSopSequence(
                exception.Value,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationFailedFailureCode));
        }

        [Fact]
        public async void StoreSinglepart_ServerShouldReturnOK()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFile();
            await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
            {
                await dicomFile.SaveAsync(stream);
                DicomWebResponse<DicomDataset> response = await _client.StoreAsync(stream);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async void StoreSinglepartWithStudyUID_ServerShouldReturnOK()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);
            DicomWebResponse<DicomDataset> response = await _client.StoreAsync(dicomFile, studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private (string SopInstanceUid, string RetrieveUri, string SopClassUid) ConvertToReferencedSopSequenceEntry(DicomDataset dicomDataset)
        {
            string studyInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

            string relativeUri = $"/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}";

            return (dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                new Uri(_client.HttpClient.BaseAddress, relativeUri).ToString(),
                dicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID));
        }

        private (string SopInstanceUid, string SopClassUid, ushort FailureReason) ConvertToFailedSopSequenceEntry(DicomDataset dicomDataset, ushort failureReason)
        {
            return (dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                dicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID),
                failureReason);
        }
    }
}
