// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public partial class RetrieveTransactionResourceTests
    {
        /// <summary>
        /// Test for transcoder when given input and output transfer syntax should return the content in the expected format
        /// </summary>
        /// <param name="testDataFolder"> Path to folder containing input DICOM file (Input.dcm), expected output DICOM file
        /// (ExpectedOutput.dcm) and Metadata.json which has the input and output syntaxUid and the precomputed sha256 hash of the output frames.
        /// </param>
        /// <param name="mediaType"> Expected type of output </param>
        /// <param name="transferSyntax"> Transfer syntax to use </param>
        [Theory]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestExplicitVRLittleEndianOriginallyJPEG2000Lossless", DicomWebConstants.ApplicationOctetStreamMediaType, null)]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestExplicitVRLittleEndianOriginallyJPEG2000Lossless", DicomWebConstants.ApplicationOctetStreamMediaType, "1.2.840.10008.1.2.1")]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestOriginalContent", DicomWebConstants.ApplicationOctetStreamMediaType, "*")]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestJPEG2000LosslessOriginallyExplicitVRLittleEndian", DicomWebConstants.ImageJpeg2000MediaType, null)]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestJPEG2000LosslessOriginallyExplicitVRLittleEndian", DicomWebConstants.ImageJpeg2000MediaType, "1.2.840.10008.1.2.4.90")]
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            await UploadTestData(testDataFolder);
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = await DicomFile.OpenAsync(transcoderTestData.InputDicomFile);

            string studyInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.SOPInstanceUID);
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, studyInstanceUid, seriesInstanceUid, sopInstanceUid, string.Join("%2C", new int[] { 1 })), UriKind.Relative);
            DicomFile outputDicomFile = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
            DicomPixelData pixelData = DicomPixelData.Create(outputDicomFile.Dataset);
            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: false, mediaType, transferSyntax);
            try
            {
                using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
                {
                    Assert.True(response.IsSuccessStatusCode);

                    int frameIndex = 0;
                    Dictionary<MultipartSection, Stream> sections = await ReadMultipart(response.Content, new CancellationTokenSource().Token);
                    foreach (MultipartSection item in sections.Keys)
                    {
                        Assert.Equal(mediaType, item.ContentType);
                        Assert.Equal(sections[item].ToByteArray(), pixelData.GetFrame(frameIndex).Data);
                        frameIndex++;
                    }
                }
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUid);
            }
        }

        [Theory]
        [InlineData(true, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // use single part instead of multiple part
        [InlineData(false, DicomWebConstants.ImagePngMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // unsupported media type image/png
        [InlineData(false, DicomWebConstants.ApplicationOctetStreamMediaType, "1.2.840.10008.1.2.4.100")] // unsupported media type MPEG2
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveFrame_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), string.Join("%2C", new int[] { 1 })), UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async Task GivenUnsupportedTransferSyntax_WhenRetrieveFrameWithOriginalTransferSyntax_ThenOriginalContentReturned()
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
            DicomWebResponse<IReadOnlyList<Stream>> response = await _client.RetrieveFramesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                dicomTransferSyntax: "*",
                frames: new[] { 1 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            VerifyFrameAreEquals(response.Value[0], dicomFile, 0);
        }

        [Fact]
        public async Task GivenUnsupportedTransferSyntax_WhenRetrieveFrame_ThenServerShouldReturnNotAcceptable()
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
            try
            {
                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveFramesAsync(
                    studyInstanceUid,
                    seriesInstanceUid,
                    sopInstanceUid,
                    dicomTransferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                    frames: new[] { 1 }));

                Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUid);
            }
        }

        [Fact]
        public async Task GivenMultipleFrames_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 3);
            DicomPixelData pixelData = DicomPixelData.Create(dicomFile1.Dataset);
            var dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier();
            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);
            try
            {
                DicomWebResponse<IReadOnlyList<Stream>> frames = await _client.RetrieveFramesAsync(dicomInstance.StudyInstanceUid, dicomInstance.SeriesInstanceUid, dicomInstance.SopInstanceUid, frames: new[] { 1, 2 }, dicomTransferSyntax: "*");
                Assert.NotNull(frames);
                Assert.Equal(HttpStatusCode.OK, frames.StatusCode);
                Assert.Equal(2, frames.Value.Count);
                Assert.Equal(KnownContentTypes.MultipartRelated, frames.Content.Headers.ContentType.MediaType);
                Assert.Equal(pixelData.GetFrame(0).Data, frames.Value[0].ToByteArray());
                Assert.Equal(pixelData.GetFrame(1).Data, frames.Value[1].ToByteArray());
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUid);
            }
        }

        [Fact]
        public async Task GivenNonExistingFrames_WhenRetrieveFrame_ThenServerShouldReturnNotFound()
        {
            (var identifier, DicomFile file) = await CreateAndStoreDicomFile(2);

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
                () => _client.RetrieveFramesAsync(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid, dicomTransferSyntax: "*", frames: new[] { 4 }));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("0.6")]
        [InlineData("-1")]
        [InlineData("1", "-1")]
        [InlineData("test")]
        [InlineData("0", "1", "invalid")]
        public async Task GivenInvalidFrames_WhenRetrievingFrame_TheServerShouldReturnBadRequest(params string[] frames)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), string.Join("%2C", frames)), UriKind.Relative);
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(
               () => _client.RetrieveFramesAsync(requestUri));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        }
    }
}
