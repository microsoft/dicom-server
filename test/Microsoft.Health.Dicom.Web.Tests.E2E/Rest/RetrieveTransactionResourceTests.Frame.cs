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
using Dicom.Imaging;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public partial class RetrieveTransactionResourceTests
    {
        [Theory]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, DicomWebConstants.ApplicationOctetStreamMediaType, null)]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, DicomWebConstants.ApplicationOctetStreamMediaType, "1.2.840.10008.1.2.1")]
        [InlineData(RequestOriginalContentTestFolder, DicomWebConstants.ApplicationOctetStreamMediaType, "*")]
        [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, DicomWebConstants.ImageJpeg2000MediaType, null)]
        [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, DicomWebConstants.ImageJpeg2000MediaType, "1.2.840.10008.1.2.4.90")]
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = await DicomFile.OpenAsync(transcoderTestData.InputDicomFile);             
            var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);

            await InternalStoreAsync(new[] { inputDicomFile });

            DicomFile outputDicomFile = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
            DicomPixelData pixelData = DicomPixelData.Create(outputDicomFile.Dataset);

            using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
                instanceId.StudyInstanceUid,
                instanceId.SeriesInstanceUid,
                instanceId.SopInstanceUid,
                mediaType,
                transferSyntax,
                frames: new[] { 1 });

            int frameIndex = 0;

            await foreach (Stream item in response)
            {
                // TODO: verify media type once https://microsofthealth.visualstudio.com/Health/_workitems/edit/75185 is done
                Assert.Equal(item.ToByteArray(), pixelData.GetFrame(frameIndex).Data);
                frameIndex++;
            }

        }

        [Theory]
        [InlineData(true, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // use single part instead of multiple part
        [InlineData(false, DicomWebConstants.ImagePngMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // unsupported media type image/png
        [InlineData(false, DicomWebConstants.ApplicationOctetStreamMediaType, "1.2.840.10008.1.2.4.100")] // unsupported media type MPEG2
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveFrame_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseRetrieveFramesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), string.Join("%2C", new int[] { 1 })), UriKind.Relative);

            using HttpRequestMessage request = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);
            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
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

            await InternalStoreAsync(new[] { dicomFile });

            // Check for series
            using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                dicomTransferSyntax: "*",
                frames: new[] { 1 });

            Stream[] results = await response.ToArrayAsync();

            Assert.Collection(
                results,
                item => Assert.Equal(item.ToByteArray(), DicomPixelData.Create(dicomFile.Dataset).GetFrame(0).Data));
        }

        [Fact]
        public async Task GivenUnsupportedTransferSyntax_WhenRetrieveFrame_ThenServerShouldReturnNotAcceptable()
        {
            string studyInstanceUid = TestUidGenerator.Generate();
            string seriesInstanceUid = TestUidGenerator.Generate();
            string sopInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                transferSyntax: DicomTransferSyntax.HEVCH265Main10ProfileLevel51.UID.UID,
                encode: false);

            await InternalStoreAsync(new[] { dicomFile });

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveFramesAsync(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                dicomTransferSyntax: DicomTransferSyntax.JPEG2000Lossless.UID.UID,
                frames: new[] { 1 }));

            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Fact]
        public async Task GivenMultipleFrames_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent()
        {
            string studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWithPixelData(studyInstanceUid, frames: 3);
            DicomPixelData pixelData = DicomPixelData.Create(dicomFile1.Dataset);
            InstanceIdentifier dicomInstance = dicomFile1.Dataset.ToInstanceIdentifier();

            await InternalStoreAsync(new[] { dicomFile1 });

            using DicomWebAsyncEnumerableResponse<Stream> response = await _client.RetrieveFramesAsync(
                dicomInstance.StudyInstanceUid,
                dicomInstance.SeriesInstanceUid,
                dicomInstance.SopInstanceUid,
                frames: new[] { 1, 2 },
                dicomTransferSyntax: "*");

            Stream[] frames = await response.ToArrayAsync();

            Assert.NotNull(frames);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, frames.Length);
            Assert.Equal(KnownContentTypes.MultipartRelated, response.ContentHeaders.ContentType.MediaType);
            Assert.Equal(pixelData.GetFrame(0).Data, frames[0].ToByteArray());
            Assert.Equal(pixelData.GetFrame(1).Data, frames[1].ToByteArray());
        }

        [Fact]
        public async Task GivenNonExistingFrames_WhenRetrieveFrame_ThenServerShouldReturnNotFound()
        {
            (InstanceIdentifier identifier, DicomFile file) = await CreateAndStoreDicomFile(2);

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
