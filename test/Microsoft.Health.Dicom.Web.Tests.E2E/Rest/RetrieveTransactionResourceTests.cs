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
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;
using NameValueHeaderValue = System.Net.Http.Headers.NameValueHeaderValue;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveTransactionResourceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private static readonly CancellationToken _defaultCancellationToken = new CancellationTokenSource().Token;

        public static readonly List<string> SupportedTransferSyntaxesFor8BitTranscoding = new List<string>
        {
            "DeflatedExplicitVRLittleEndian",
            "ExplicitVRBigEndian",
            "ExplicitVRLittleEndian",
            "ImplicitVRLittleEndian",
            "JPEG2000Lossless",
            "JPEG2000Lossy",
            "JPEGProcess1",
            "JPEGProcess2_4",
            "RLELossless",
        };

        public static readonly List<string> SupportedTransferSyntaxesForOver8BitTranscoding = new List<string>
        {
            "DeflatedExplicitVRLittleEndian",
            "ExplicitVRBigEndian",
            "ExplicitVRLittleEndian",
            "ImplicitVRLittleEndian",
            "RLELossless",
        };

        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForStudy_ThenServerShouldReturnNotFound()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyAsync(TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenStoredInstance_WhenRetrieveRequestForStudy_ThenServerShouldReturnInstancesInStudy()
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile();

            DicomWebResponse<IReadOnlyList<DicomFile>> instancesInStudy = await _client.RetrieveStudyAsync(identifier.StudyInstanceUid);
            ValidateRetrieveTransaction(instancesInStudy, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: false, file);
        }

        [Fact]
        public async Task GivenNoStoredInstances_WhenRetrieveRequestForSeries_ThenServerShouldReturnNotFound()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenStoredInstance_WhenRetrieveRequestForSeries_ThenServerShouldReturnInstancesInSeries()
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile();

            DicomWebResponse<IReadOnlyList<DicomFile>> instancesInSeries = await _client.RetrieveSeriesAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid);
            ValidateRetrieveTransaction(instancesInSeries, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: false, file);
        }

        [Fact]
        public async Task GivenStoredInstance_WhenRetrieveRequestForDifferentInstance_ThenServerShouldReturnNotFound()
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile();

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.RetrieveInstanceAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenStoredInstance_WhenRetrieveRequestForInstance_ThenServerShouldReturnInstance()
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile();

            DicomWebResponse<IReadOnlyList<DicomFile>> instances = await _client.RetrieveInstanceAsync(
                identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid);
            ValidateRetrieveTransaction(instances, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: true, file);
        }

        [Fact]
        public async Task GivenInstanceWithFrames_WhenRetrieveRequestForNonExistingFrameInInstance_ThenServerShouldReturnNotFound()
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile(2);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.RetrieveFramesAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, dicomTransferSyntax: "*", frames: new[] { 4 }));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1, 2)]
        public async Task GivenInstanceWithFrames_WhenRetrieveRequestForFrameInInstance_ValidateReturnedHeaders(params int[] frames)
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile(2);
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, string.Join("%2C", frames)), UriKind.Relative);
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                // Set accept header to multipart/related; type="application/octet-stream"
                MediaTypeWithQualityHeaderValue multipartHeader = new MediaTypeWithQualityHeaderValue(KnownContentTypes.MultipartRelated);
                NameValueHeaderValue contentHeader = new NameValueHeaderValue("type", "\"" + KnownContentTypes.ApplicationOctetStream + "\"");
                multipartHeader.Parameters.Add(contentHeader);

                string transferSyntaxHeader = ";transfer-syntax=\"*\"";
                request.Headers.TryAddWithoutValidation("Accept", $"{multipartHeader.ToString()}{transferSyntaxHeader}");

                request.Headers.Add("transfer-syntax", "*");

                using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _defaultCancellationToken))
                {
                    Assert.True(response.IsSuccessStatusCode);

                    await using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        // Open stream in response message's content and read using multipart reader.
                        MultipartSection part;
                        var media = MediaTypeHeaderValue.Parse(response.Content.Headers.ContentType.ToString());
                        var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

                        while ((part = await multipartReader.ReadNextSectionAsync(_defaultCancellationToken)) != null)
                        {
                            // Validate header on individual parts is application/octet-stream.
                            Assert.Equal(KnownContentTypes.ApplicationOctetStream, part.ContentType);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task GivenInstanceWithFrames_WhenRetrieveRequestForFramesInInstance_ThenServerShouldReturnRequestedFrames()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 2);
            var dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier();
            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);

            DicomWebResponse<IReadOnlyList<Stream>> frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: new[] { 1, 2 });
            Assert.NotNull(frames);
            Assert.Equal(HttpStatusCode.OK, frames.StatusCode);
            Assert.Equal(2, frames.Value.Count);
            Assert.Equal(KnownContentTypes.MultipartRelated, frames.Content.Headers.ContentType.MediaType);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(0), frames.Value[0]);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(1), frames.Value[1]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("1.2.840.10008.1.2.1")]
        public async Task GivenInstanceWithFrames_WhenRetrieveRequestForFramesInInstanceWithSupportedTransferSyntax_ThenServerShouldReturnRequestedFrames(string transferSyntaxUid)
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 2);
            var dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier();
            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);

            DicomWebResponse<IReadOnlyList<Stream>> frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: new[] { 1, 2 }, dicomTransferSyntax: transferSyntaxUid);
            Assert.NotNull(frames);
            Assert.Equal(HttpStatusCode.OK, frames.StatusCode);
            Assert.Equal(2, frames.Value.Count);
            Assert.Equal(KnownContentTypes.MultipartRelated, frames.Content.Headers.ContentType.MediaType);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(0), frames.Value[0]);
            AssertPixelDataEqual(DicomPixelData.Create(dicomFile1.Dataset).GetFrame(1), frames.Value[1]);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("0", "1", "invalid")]
        [InlineData("0.6", "1")]
        public async Task GivenARequestWithNonIntegerFrames_WhenRetrievingFrames_TheServerShouldReturnBadRequest(params string[] frames)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), string.Join("%2C", frames)), UriKind.Relative);
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
               () => _client.RetrieveFramesAsync(requestUri));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1, 2, -1)]
        public async Task GivenARequestWithFrameLessThanOrEqualTo0_WhenRetrievingFrames_TheServerShouldReturnBadRequest(params int[] frames)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveFramesAsync(
                studyInstanceUid: TestUidGenerator.Generate(),
                seriesInstanceUid: TestUidGenerator.Generate(),
                sopInstanceUid: TestUidGenerator.Generate(),
                frames: frames));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }

        public static IEnumerable<object[]> GetInvalidTransferSyntaxData()
        {
            yield return new object[] { DicomTransferSyntax.ExplicitVRLittleEndian.ToString() };
            yield return new object[] { "unknown" };
            yield return new object[] { "&&5" };
        }

        [Theory]
        [MemberData(nameof(GetInvalidTransferSyntaxData))]
        public async Task GivenARequestWithInvalidTransferSyntax_WhenRetrievingStudy_TheServerShouldReturnBadRequest(string transferSyntax)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyAsync(TestUidGenerator.Generate(), transferSyntax));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTransferSyntaxData))]
        public async Task GivenARequestWithInvalidTransferSyntax_WhenRetrievingSeries_TheServerShouldReturnBadRequest(string transferSyntax)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), transferSyntax));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTransferSyntaxData))]
        public async Task GivenARequestWithInvalidTransferSyntax_WhenRetrievingInstance_TheServerShouldReturnBadRequest(string transferSyntax)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.RetrieveInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), transferSyntax));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTransferSyntaxData))]
        public async Task GivenARequestWithInvalidTransferSyntax_WhenRetrievingFrames_TheServerShouldReturnBadRequest(string transferSyntax)
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveFramesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), transferSyntax, frames: new[] { 1 }));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Theory]
        [InlineData("helloworld")]
        [InlineData("traNSFer  -sYNTAx=  *")]
        [InlineData("   traNSFer  -sYNTAx=  *")]
        public async Task GivenARequestWithInvaidTransferSyntaxFormatting_WhenRetrievingInstance_TheServerShouldReturnNotAcceptable(string transferSyntaxHeader)
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile();
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid), UriKind.Relative);
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                // Set accept header to multipart/related; type="application/dicom"
                MediaTypeWithQualityHeaderValue multipartHeader = new MediaTypeWithQualityHeaderValue(KnownContentTypes.MultipartRelated);
                NameValueHeaderValue contentHeader = new NameValueHeaderValue("type", "\"" + KnownContentTypes.ApplicationDicom + "\"");
                multipartHeader.Parameters.Add(contentHeader);

                request.Headers.TryAddWithoutValidation("Accept", $"{multipartHeader.ToString()};{transferSyntaxHeader}");

                using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _defaultCancellationToken))
                {
                    Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData("traNSFer-sYNTAx=\"*\";traNSFer-sYNTAx=*")]
        public async Task GivenARequestWithMutipleTransferSyntaxHeaders_WhenRetrievingInstance_TheServerShouldReturnBadRequest(string transferSyntaxHeader)
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile();
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid), UriKind.Relative);
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                // Set accept header to multipart/related; type="application/dicom"
                MediaTypeWithQualityHeaderValue multipartHeader = new MediaTypeWithQualityHeaderValue(KnownContentTypes.MultipartRelated);
                NameValueHeaderValue contentHeader = new NameValueHeaderValue("type", "\"" + KnownContentTypes.ApplicationDicom + "\"");
                multipartHeader.Parameters.Add(contentHeader);

                request.Headers.TryAddWithoutValidation("Accept", $"{multipartHeader.ToString()};{transferSyntaxHeader}");

                using (HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _defaultCancellationToken))
                {
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GivenStoredDicomFileWithNoContent_WhenRetrieved_TheFileIsRetrievedCorrectly()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid);
            var dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier();
            DicomWebResponse<DicomDataset> response = await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);

            DicomSequence successSequence = response.Value.GetSequence(DicomTag.ReferencedSOPSequence);

            string studyRetrieveLocation = response.Value.GetSingleValue<string>(DicomTag.RetrieveURL);
            string instanceRetrieveLocation = successSequence.Items[0].GetSingleValue<string>(DicomTag.RetrieveURL);

            DicomWebResponse<IReadOnlyList<DicomFile>> studyByUrlRetrieve = await _client.RetrieveInstancesAsync(new Uri(studyRetrieveLocation));
            ValidateRetrieveTransaction(studyByUrlRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: false, dicomFile1);

            DicomWebResponse<IReadOnlyList<DicomFile>> instanceByUrlRetrieve = await _client.RetrieveInstancesAsync(new Uri(instanceRetrieveLocation), true);
            ValidateRetrieveTransaction(instanceByUrlRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: true, dicomFile1);

            DicomWebResponse<IReadOnlyList<DicomFile>> studyRetrieve = await _client.RetrieveStudyAsync(dicomInstance.StudyInstanceUid);
            ValidateRetrieveTransaction(studyRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: false, dicomFile1);

            DicomWebResponse<IReadOnlyList<DicomFile>> seriesRetrieve = await _client.RetrieveSeriesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid);
            ValidateRetrieveTransaction(seriesRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: false, dicomFile1);

            DicomWebResponse<IReadOnlyList<DicomFile>> instanceRetrieve = await _client.RetrieveInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            ValidateRetrieveTransaction(instanceRetrieve, HttpStatusCode.OK, DicomTransferSyntax.ExplicitVRLittleEndian, singleInstance: true, dicomFile1);
        }

        [Theory(Skip = "The file fails with validation.")]
        [InlineData("1.2.840.10008.1.2.4.100", HttpStatusCode.NotAcceptable)] // Unsupported conversion - a video codec
        [InlineData("Bogus TS", HttpStatusCode.BadRequest)] // A non-existent codec
        [InlineData("1.2.840.10008.5.1.4.1.1.1", HttpStatusCode.BadRequest)] // Valid UID, but not a transfer syntax
        public async Task GivenAnUnsupportedTransferSyntax_WhenRetrievingStudy_NotAcceptableIsReturned(string transferSyntax, HttpStatusCode expectedStatusCode)
        {
            IEnumerable<DicomFile> dicomFiles = Samples.GetDicomFilesForTranscoding();
            DicomFile dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == "ExplicitVRLittleEndian"));
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();

            try
            {
                await _client.StoreAsync(new[] { dicomFile });

                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, transferSyntax));

                Assert.Equal(expectedStatusCode, exception.StatusCode);
            }
            finally
            {
                await _client.DeleteInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            }
        }

        // Test that if no TS specified, we return the original TS w/o transcoding -
        // http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_8.7.3.5.2:S
        // The wildcard value "*" indicates that the user agent will accept any Transfer Syntax.
        // This allows, for example, the origin server to respond without needing to encode an
        // existing representation to a new Transfer Syntax, or to respond with the
        // Explicit VR Little Endian Transfer Syntax regardless of the Transfer Syntax stored.
        [Fact]
        public async Task GivenAnUnsupportedTransferSyntax_WhenWildCardTsSpecified_OriginalImageReturned()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
                encode: false);

            await _client.StoreAsync(new[] { dicomFile });

            // Check for series
            DicomWebResponse<IReadOnlyList<DicomFile>> seriesResponse = await _client.RetrieveSeriesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                "*");

            Assert.Equal(HttpStatusCode.OK, seriesResponse.StatusCode);
            Assert.Equal(DicomTransferSyntax.HEVCH265Main10ProfileLevel51, seriesResponse.Value.Single().Dataset.InternalTransferSyntax);

            // Check for frame
            DicomWebResponse<IReadOnlyList<Stream>> frameResponse = await _client.RetrieveFramesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                dicomTransferSyntax: "*",
                frames: new[] { 1 });

            Assert.Equal(HttpStatusCode.OK, frameResponse.StatusCode);
            Assert.NotEqual(0, frameResponse.Value.Single().Length);
        }

        [Fact]
        public async Task GivenSupportedTransferSyntax_WhenNoTsSpecified_DefaultTsReturned()
        {
            var seriesInstanceUid = TestUidGenerator.Generate();
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.DeflatedExplicitVRLittleEndian.UID.UID);

            try
            {
                await _client.StoreAsync(new[] { dicomFile });

                DicomWebResponse<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveSeriesAsync(
                    studyInstanceUid,
                    seriesInstanceUid);

                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.Equal(DicomTransferSyntax.DeflatedExplicitVRLittleEndian, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);
            }
            finally
            {
                await _client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);
            }
        }

        [Fact]
        public async Task GivenSupportedTransferSyntax_WhenSupportedWildcardTsIsSpecified_OriginalTsReturned()
        {
            var seriesInstanceUid = TestUidGenerator.Generate();
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.DeflatedExplicitVRLittleEndian.UID.UID);

            try
            {
                await _client.StoreAsync(new[] { dicomFile });

                DicomWebResponse<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveSeriesAsync(
                    studyInstanceUid,
                    seriesInstanceUid,
                    dicomTransferSyntax: "*");

                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.Equal(DicomTransferSyntax.DeflatedExplicitVRLittleEndian, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);
            }
            finally
            {
                await _client.DeleteSeriesAsync(studyInstanceUid, seriesInstanceUid);
            }
        }

        [Fact]
        public async Task GivenAMixOfTransferSyntaxes_WhenSomeAreSupported_NotAcceptableIsReturned()
        {
            var seriesInstanceUid = TestUidGenerator.Generate();
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
                encode: false);

            DicomFile dicomFile3 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ImplicitVRLittleEndian.UID.UID);

            await _client.StoreAsync(new[] { dicomFile1, dicomFile2, dicomFile3 });

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                DicomTransferSyntax.JPEG2000Lossy.UID.UID));

            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        public static IEnumerable<object[]> Get8BitTranscoderCombos()
        {
            List<string> fromList = SupportedTransferSyntaxesFor8BitTranscoding;
            List<string> toList = SupportedTransferSyntaxesFor8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        public static IEnumerable<object[]> Get16BitTranscoderCombos()
        {
            List<string> fromList = SupportedTransferSyntaxesForOver8BitTranscoding;
            List<string> toList = SupportedTransferSyntaxesForOver8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        [Theory(Skip = "The file fails with validation")]
        [MemberData(nameof(Get16BitTranscoderCombos))]
        public async Task GivenSupported16bitTransferSyntax_WhenRetrievingStudyAndAskingForConversion_OKIsReturned(
            string tsFrom,
            string tsTo)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWith16BitPixelData(transferSyntax: ((DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsFrom).GetValue(null)).UID.UID);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();

            try
            {
                await _client.StoreAsync(new[] { dicomFile });

                var expectedTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);

                DicomWebResponse<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid,
                    dicomInstance.SeriesInstanceUid,
                    dicomInstance.SopInstanceUid,
                    expectedTransferSyntax.UID.UID);

                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.Equal(expectedTransferSyntax, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);

                DicomWebResponse<IReadOnlyList<Stream>> framesResponse = await _client.RetrieveFramesAsync(
                    dicomInstance.StudyInstanceUid,
                    dicomInstance.SeriesInstanceUid,
                    dicomInstance.SopInstanceUid,
                    expectedTransferSyntax.UID.UID,
                    frames: new[] { 1 });

                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.NotEqual(0, framesResponse.Value.Single().Length);
            }
            finally
            {
                await _client.DeleteInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            }
        }

        [Theory(Skip = "The file fails with validation.")]
        [MemberData(nameof(Get8BitTranscoderCombos))]
        public async Task GivenSupported8bitTransferSyntax_WhenRetrievingStudyAndAskingForConversion_OKIsReturned(
            string tsFrom,
            string tsTo)
        {
            IEnumerable<DicomFile> dicomFiles = Samples.GetDicomFilesForTranscoding();
            DicomFile dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == tsFrom));
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();

            try
            {
                await _client.StoreAsync(new[] { dicomFile });

                var expectedTransferSyntax = (DicomTransferSyntax)typeof(DicomTransferSyntax).GetField(tsTo).GetValue(null);

                DicomWebResponse<IReadOnlyList<DicomFile>> retrieveResponse = await _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, expectedTransferSyntax.UID.UID);

                Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
                Assert.Equal(expectedTransferSyntax, retrieveResponse.Value.Single().Dataset.InternalTransferSyntax);
                Assert.NotNull(retrieveResponse.Value.Single());
            }
            finally
            {
                await _client.DeleteInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            }
        }

        [Theory(Skip = "The file fails with validation.")]
        [InlineData("1.2.840.10008.1.2.4.91")] // JPEG Process 1 - should work, but doesn't for this particular image. Not officially supported
        public async Task GivenAnExceptionDuringTranscoding_WhenRetrievingStudy_EmptyStreamIsReturned(string transferSyntax)
        {
            IEnumerable<DicomFile> dicomFiles = Samples.GetSampleDicomFiles();
            DicomFile dicomFile = dicomFiles.FirstOrDefault(f => (Path.GetFileNameWithoutExtension(f.File.Name) == "XRJPEGProcess1"));
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();

            try
            {
                await _client.StoreAsync(new[] { dicomFile });

                DicomWebResponse<IReadOnlyList<DicomFile>> response = await _client.RetrieveInstanceAsync(
                    dicomInstance.StudyInstanceUid,
                    dicomInstance.SeriesInstanceUid,
                    dicomInstance.SopInstanceUid,
                    transferSyntax);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Null(response.Value.Single());
            }
            finally
            {
                await _client.DeleteInstanceAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid);
            }
        }

        [Theory]
        [InlineData("application/data")]
        [InlineData("application/json")]
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingResource_NotAcceptableIsReturned(string acceptHeader)
        {
            // Study
            await _client.ValidateResponseStatusCodeAsync(
                string.Format(DicomWebConstants.BasStudyUriFormat, TestUidGenerator.Generate()),
                acceptHeader,
                HttpStatusCode.NotAcceptable);

            // Series
            await _client.ValidateResponseStatusCodeAsync(
                string.Format(DicomWebConstants.BaseSeriesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate()),
                acceptHeader,
                HttpStatusCode.NotAcceptable);

            // Instance
            await _client.ValidateResponseStatusCodeAsync(
                string.Format(DicomWebConstants.BaseInstanceUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()),
                acceptHeader,
                HttpStatusCode.NotAcceptable);
        }

        [Theory]
        [InlineData("application/dicom")]
        [InlineData("application/data")]
        [InlineData("application/json")]
        public async Task GivenAnIncorrectAcceptHeader_WhenRetrievingFrames_NotAcceptableIsReturned(string acceptHeader)
        {
            await _client.ValidateResponseStatusCodeAsync(
                string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1),
                acceptHeader,
                HttpStatusCode.NotAcceptable);
        }

        private async Task<(InstanceIdentifier, DicomFile)> CreateAndStoreDicomFile(int numberOfFrames = 0)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: numberOfFrames);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            await _client.StoreAsync(new[] { dicomFile }, dicomInstance.StudyInstanceUid);

            return (dicomInstance, dicomFile);
        }

        private void ValidateRetrieveTransaction(
            DicomWebResponse<IReadOnlyList<DicomFile>> response,
            HttpStatusCode expectedStatusCode,
            DicomTransferSyntax expectedTransferSyntax,
            bool singleInstance = false,
            params DicomFile[] expectedFiles)
        {
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedFiles.Length, response.Value.Count);

            if (singleInstance)
            {
                Assert.Equal(KnownContentTypes.ApplicationDicom, response.Content.Headers.ContentType.MediaType);
            }
            else
            {
                Assert.Equal(KnownContentTypes.MultipartRelated, response.Content.Headers.ContentType.MediaType);
            }

            for (var i = 0; i < expectedFiles.Length; i++)
            {
                DicomFile expectedFile = expectedFiles[i];
                var expectedInstance = expectedFile.Dataset.ToInstanceIdentifier();
                DicomFile actualFile = response.Value.First(x => x.Dataset.ToInstanceIdentifier().Equals(expectedInstance));

                Assert.Equal(expectedTransferSyntax, response.Value[i].Dataset.InternalTransferSyntax);

                // If the same transfer syntax as original, the files should be exactly the same
                if (expectedFile.Dataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
                {
                    var expectedFileArray = DicomFileToByteArray(expectedFile);
                    var actualFileArray = DicomFileToByteArray(actualFile);

                    Assert.Equal(expectedFileArray.Length, actualFileArray.Length);

                    for (var ii = 0; ii < expectedFileArray.Length; ii++)
                    {
                        Assert.Equal(expectedFileArray[ii], actualFileArray[ii]);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private byte[] DicomFileToByteArray(DicomFile dicomFile)
        {
            using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                dicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static void AssertPixelDataEqual(IByteBuffer expectedPixelData, Stream actualPixelData)
        {
            Assert.Equal(expectedPixelData.Size, actualPixelData.Length);
            Assert.Equal(0, actualPixelData.Position);
            for (var i = 0; i < expectedPixelData.Size; i++)
            {
                Assert.Equal(expectedPixelData.Data[i], actualPixelData.ReadByte());
            }
        }
    }
}
