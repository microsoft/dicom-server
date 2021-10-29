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
using FellowOakDicom;
using EnsureThat;
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

        private readonly IDicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public StoreTransactionTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            EnsureArg.IsNotNull(fixture, nameof(fixture));
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenRandomContent_WhenStoring_TheServerShouldReturnConflict()
        {
            await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { stream }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        }

        [Fact]
        public async Task GivenARequestWithInvalidStudyInstanceUID_WhenStoring_TheServerShouldReturnBadRequest()
        {
            await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { stream }, studyInstanceUid: new string('b', 65)));

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetIncorrectAcceptHeaders))]
        public async Task GivenAnIncorrectAcceptHeader_WhenStoring_TheServerShouldReturnNotAcceptable(string acceptHeader)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(DicomApiVersions.Latest + DicomWebConstants.StudiesUriString, UriKind.Relative));
            request.Headers.Add(HeaderNames.Accept, acceptHeader);

            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Fact]
        public async Task GivenAnNonMultipartRequest_WhenStoring_TheServerShouldReturnUnsupportedMediaType()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(DicomApiVersions.Latest + DicomWebConstants.StudiesUriString, UriKind.Relative));
            request.Headers.Add(HeaderNames.Accept, DicomWebConstants.MediaTypeApplicationDicomJson.MediaType);

            var multiContent = new MultipartContent("form");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));
            request.Content = multiContent;

            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact]
        public async Task GivenAMultipartRequestWithNoContent_WhenStoring_TheServerShouldReturnNoContent()
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

            using DicomWebResponse response = await _client.StoreAsync(multiContent);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task GivenAMultipartRequestWithEmptyContent_WhenStoring_TheServerShouldReturnConflict()
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.StoreAsync(multiContent));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        }

        [Fact]
        public async Task GivenAMultipartRequestWithAnInvalidMultipartSection_WhenStoring_TheServerShouldReturnAccepted()
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

            var byteContent = new ByteArrayContent(Array.Empty<byte>());
            byteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
            multiContent.Add(byteContent);

            string studyInstanceUID = TestUidGenerator.Generate();

            try
            {
                DicomFile validFile = Samples.CreateRandomDicomFile(studyInstanceUID);

                await using (MemoryStream stream = _recyclableMemoryStreamManager.GetStream())
                {
                    await validFile.SaveAsync(stream);

                    var validByteContent = new ByteArrayContent(stream.ToArray());
                    validByteContent.Headers.ContentType = DicomWebConstants.MediaTypeApplicationDicom;
                    multiContent.Add(validByteContent);
                }

                using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(multiContent);

                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                ValidationHelpers.ValidateReferencedSopSequence(
                    await response.GetValueAsync(),
                    ConvertToReferencedSopSequenceEntry(validFile.Dataset));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async Task GivenAMultipartRequestWithTypeParameterAndFirstSectionWithoutContentType_WhenStoring_TheServerShouldReturnOK()
        {
            var multiContent = new MultipartContent("related");
            multiContent.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("type", $"\"{DicomWebConstants.MediaTypeApplicationDicom.MediaType}\""));

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

                using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(multiContent);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                ValidationHelpers.ValidateReferencedSopSequence(
                    await response.GetValueAsync(),
                    ConvertToReferencedSopSequenceEntry(dicomFile.Dataset));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async Task GivenAllDifferentStudyInstanceUIDs_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnConflict()
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile();
            DicomFile dicomFile2 = Samples.CreateRandomDicomFile();

            var studyInstanceUID = TestUidGenerator.Generate();

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(
                new[] { dicomFile1, dicomFile2 }, studyInstanceUid: studyInstanceUID));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

            DicomDataset dataset = exception.ResponseDataset;

            Assert.NotNull(dataset);
            Assert.True(dataset.Count() == 1);

            ValidationHelpers.ValidateFailedSopSequence(
                dataset,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, MismatchStudyInstanceUidFailureCode),
                ConvertToFailedSopSequenceEntry(dicomFile2.Dataset, MismatchStudyInstanceUidFailureCode));
        }

        [Fact]
        [Trait("Category", "bvt")]
        public async Task GivenOneDifferentStudyInstanceUID_WhenStoringWithProvidedStudyInstanceUID_TheServerShouldReturnAccepted()
        {
            var studyInstanceUID1 = TestUidGenerator.Generate();
            var studyInstanceUID2 = TestUidGenerator.Generate();

            try
            {
                DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID1);
                DicomFile dicomFile2 = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID2);

                using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile1, dicomFile2 }, studyInstanceUid: studyInstanceUID1);

                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                DicomDataset dataset = await response.GetValueAsync();

                Assert.NotNull(dataset);
                Assert.True(dataset.Count() == 3);

                Assert.EndsWith($"studies/{studyInstanceUID1}", dataset.GetSingleValue<string>(DicomTag.RetrieveURL));

                ValidationHelpers.ValidateReferencedSopSequence(
                    dataset,
                    ConvertToReferencedSopSequenceEntry(dicomFile1.Dataset));

                ValidationHelpers.ValidateFailedSopSequence(
                    dataset,
                    ConvertToFailedSopSequenceEntry(dicomFile2.Dataset, MismatchStudyInstanceUidFailureCode));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID1);
            }
        }

        [Fact]
        public async Task GivenDatasetWithDuplicateIdentifiers_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, studyInstanceUID);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { dicomFile1 }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

            DicomDataset dataset = exception.ResponseDataset;

            Assert.False(dataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

            ValidationHelpers.ValidateFailedSopSequence(
                dataset,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationFailedFailureCode));
        }

        [Fact]
        public async Task GivenExistingDataset_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();

            try
            {
                DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID);

                using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile1 });

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                DicomDataset dataset = await response.GetValueAsync();

                ValidationHelpers.ValidateReferencedSopSequence(
                    dataset,
                    ConvertToReferencedSopSequenceEntry(dicomFile1.Dataset));

                Assert.False(dataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence _));

                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                    () => _client.StoreAsync(new[] { dicomFile1 }));

                Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

                ValidationHelpers.ValidateFailedSopSequence(
                    exception.ResponseDataset,
                    ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, SopInstanceAlreadyExistsFailureCode));
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUID);
            }
        }

        [Fact]
        public async Task GivenDatasetWithInvalidVrValue_WhenStoring_TheServerShouldReturnConflict()
        {
            var studyInstanceUID = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithInvalidVr(studyInstanceUID);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { dicomFile1 }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
            Assert.False(exception.ResponseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

            ValidationHelpers.ValidateFailedSopSequence(
                exception.ResponseDataset,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationFailedFailureCode));
        }

        [Theory]
        [InlineData("abc.123")]
        [InlineData("11|")]
        public async Task GivenDatasetWithInvalidUid_WhenStoring_TheServerShouldReturnConflict(string studyInstanceUID)
        {
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUID, validateItems: false);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.StoreAsync(new[] { dicomFile1 }));

            Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);

            Assert.False(exception.ResponseDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence _));

            ValidationHelpers.ValidateFailedSopSequence(
                exception.ResponseDataset,
                ConvertToFailedSopSequenceEntry(dicomFile1.Dataset, ValidationFailedFailureCode));
        }

        [Fact]
        [Trait("Category", "bvt")]
        public async Task StoreSinglepart_ServerShouldReturnOK()
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFile();

            await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
            await dicomFile.SaveAsync(stream);

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(stream);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task StoreSinglepartWithStudyUID_ServerShouldReturnOK()
        {
            var studyInstanceUID = TestUidGenerator.Generate();
            DicomFile dicomFile = Samples.CreateRandomDicomFile(studyInstanceUid: studyInstanceUID);

            using DicomWebResponse<DicomDataset> response = await _client.StoreAsync(dicomFile, studyInstanceUid: studyInstanceUID);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public static IEnumerable<object[]> GetIncorrectAcceptHeaders
        {
            get
            {
                yield return new object[] { "application/dicom" };
                yield return new object[] { "application/data" };
            }
        }

        private (string SopInstanceUid, string RetrieveUri, string SopClassUid) ConvertToReferencedSopSequenceEntry(DicomDataset dicomDataset)
        {
            string studyInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

            string relativeUri = $"{DicomApiVersions.Latest}/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}";

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
