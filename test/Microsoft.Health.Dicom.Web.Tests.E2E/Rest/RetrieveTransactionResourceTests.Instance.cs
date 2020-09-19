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
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
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
        [Theory]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestOriginalContent", DicomWebConstants.ApplicationDicomMediaType, "*")]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestExplicitVRLittleEndianOriginallyJPEG2000Lossless", DicomWebConstants.ApplicationDicomMediaType, null)]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestExplicitVRLittleEndianOriginallyJPEG2000Lossless", DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.1")]
        [InlineData(@"TestFiles\RetrieveResourcesAcceptanceTests\RequestJPEG2000LosslessOriginallyExplicitVRLittleEndian", DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.90")]
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            await UploadTestData(testDataFolder);
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            var instanceId = inputDicomFile.Dataset.ToInstanceIdentifier();
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, instanceId.SopInstanceUid), UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: true, mediaType, transferSyntax);
            try
            {
                using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
                {
                    Assert.True(response.IsSuccessStatusCode);
                    Assert.Equal(mediaType, response.Content.Headers.ContentType.MediaType);

                    // TODO: abstract this as a method
                    var memoryStream = new MemoryStream();
                    await response.Content.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    DicomFile actual = DicomFile.Open(memoryStream);
                    DicomFile expected = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
                    Assert.Equal(expected, actual, new DicomFileEqualityComparer());
                }
            }
            finally
            {
                await _client.DeleteStudyAsync(instanceId.StudyInstanceUid);
            }
        }

        [Theory]
        [InlineData(false, DicomWebConstants.ApplicationDicomMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // use multipe part instead of single part
        [InlineData(true, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // unsupported media type image/png
        [InlineData(true, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100")] // unsupported transfer syntax MPEG2
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveInstance_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()), UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async Task GivenUnsupportedInternalTransferSyntax_WhenRetrieveInstance_ThenServerShouldReturnNotAcceptable()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await _client.StoreAsync(new[] { dicomFile });

            try
            {
                DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
                Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUid);
            }
        }

        [Fact]
        public async Task GivenUnsupportedInternalTransferSyntax_WhenRetrieveInstanceWithOriginalTransferSyntax_ThenServerShouldReturnOriginalContent()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                sopInstanceUid,
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await _client.StoreAsync(new[] { dicomFile });

            try
            {
                DicomWebResponse<IReadOnlyList<DicomFile>> instancesInStudy = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
                Assert.Equal(1, instancesInStudy.Value.Count);
                Assert.Equal(dicomFile.ToByteArray(), instancesInStudy.Value[0].ToByteArray());
            }
            finally
            {
                await _client.DeleteStudyAsync(studyInstanceUid);
            }
        }

        [Fact]
        public async Task GivenNonExistingInstance_WhenRetrieveInstance_ThenServerShouldReturnNotFound()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenInstanceWithoutPixelData_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            var sopInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            await _client.StoreAsync(new[] { dicomFile1 }, studyInstanceUid);

            DicomWebResponse<IReadOnlyList<DicomFile>> instanceRetrieve = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");

            Assert.Equal(dicomFile1.ToByteArray(), instanceRetrieve.Value[0].ToByteArray());
        }
    }
}
