// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
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
        [InlineData(RequestOriginalContentTestFolder, "*")]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, null)]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, "1.2.840.10008.1.2.1")]
        [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, "1.2.840.10008.1.2.4.90")]
        public async Task GivenSinglePartAcceptHeader_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(string testDataFolder, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);

            await InternalStoreAsync(new[] { inputDicomFile });

            using DicomWebResponse<DicomFile> response = await _client.RetrieveInstanceAsync(instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, instanceId.SopInstanceUid, transferSyntax);

            Assert.Equal(DicomWebConstants.ApplicationDicomMediaType, response.ContentHeaders.ContentType.MediaType);

            var actual = await response.GetValueAsync();
            var expected = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
            Assert.Equal(expected, actual, new DicomFileEqualityComparer(
                ignoredTags: new[]
                {
                    DicomTag.ImplementationVersionName,  // Version name is updated as we update fo-dicom
                    DicomTag.StudyInstanceUID,
                    DicomTag.SeriesInstanceUID,
                    DicomTag.SOPInstanceUID
                }));

        }

        [Theory]
        [InlineData(RequestOriginalContentTestFolder, "*")]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, null)]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, "1.2.840.10008.1.2.1")]
        public async Task GivenMultipartAcceptHeader_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(string testDataFolder, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);

            await InternalStoreAsync(new[] { inputDicomFile });

            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, instanceId.SopInstanceUid), UriKind.Relative);

            using HttpRequestMessage request = new HttpRequestMessageBuilder().Build(requestUri, singlePart: false, DicomWebConstants.ApplicationDicomMediaType, transferSyntax);
            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData(true, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // unsupported media type image/png
        [InlineData(true, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100")] // unsupported transfer syntax MPEG2
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveInstance_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate()), UriKind.Relative);

            using HttpRequestMessage request = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);
            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
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

            await InternalStoreAsync(new[] { dicomFile });
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Fact]
        [Trait("Category", "bvt")]
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

            await InternalStoreAsync(new[] { dicomFile });

            using DicomWebResponse<DicomFile> instancesInStudy = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
            Assert.Equal(dicomFile.ToByteArray(), (await instancesInStudy.GetValueAsync()).ToByteArray());
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
            await InternalStoreAsync(new[] { dicomFile1 });

            using DicomWebResponse<DicomFile> instanceRetrieve = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
            Assert.Equal(dicomFile1.ToByteArray(), (await instanceRetrieve.GetValueAsync()).ToByteArray());
        }
    }
}
