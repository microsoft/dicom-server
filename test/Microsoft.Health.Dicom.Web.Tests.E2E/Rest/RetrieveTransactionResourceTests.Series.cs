// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
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
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveSeries_ThenServerShouldReturnExpectedContent(string testDataFolder, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            var instanceId = RandomizeInstanceIdentifier(inputDicomFile.Dataset);
            await InternalStoreAsync(new[] { inputDicomFile });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, transferSyntax);
            Assert.Equal(DicomWebConstants.MultipartRelatedMediaType, response.ContentHeaders.ContentType.MediaType);

            await foreach (DicomFile actual in response)
            {
                // TODO: verify media type once https://microsofthealth.visualstudio.com/Health/_workitems/edit/75185 is done
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
        }

        [Theory]
        [InlineData(true, DicomWebConstants.ApplicationDicomMediaType, DicomWebConstants.OriginalDicomTransferSyntax, "")] // use single part instead of multiple part
        [InlineData(false, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax, "")] // unsupported media type image/png
        [InlineData(false, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100", "")] // unsupported media type MPEG2
        [InlineData(true, DicomWebConstants.ApplicationDicomMediaType, DicomWebConstants.OriginalDicomTransferSyntax, PrereleaseV1VersionPath)] // use single part instead of multiple part
        [InlineData(false, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax, PrereleaseV1VersionPath)] // unsupported media type image/png
        [InlineData(false, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100", PrereleaseV1VersionPath)] // unsupported media type MPEG2
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveSeries_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax, string versionPath)
        {
            var requestUri = new Uri(versionPath + string.Format(DicomWebConstants.BaseSeriesUriFormat, TestUidGenerator.Generate(), TestUidGenerator.Generate()), UriKind.Relative);

            using HttpRequestMessage request = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);
            using HttpResponseMessage response = await _client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Fact]
        public async Task GivenMultipleInstances_WhenRetrieveSeries_ThenServerShouldReturnExpectedInstances()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile3 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            await InternalStoreAsync(new[] { dicomFile1, dicomFile2, dicomFile3 });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid);

            DicomFile[] instancesInStudy = await response.ToArrayAsync();

            Assert.Equal(2, instancesInStudy.Length);

            byte[][] actual = instancesInStudy.Select(item => item.ToByteArray()).ToArray();

            Assert.Contains(dicomFile1.ToByteArray(), actual);
            Assert.Contains(dicomFile2.ToByteArray(), actual);
        }

        [Fact]
        public async Task GivenMultipleInstancesWithMixTransferSyntax_WhenRetrieveSeries_ThenServerShouldReturnNotAcceptable()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await InternalStoreAsync(new[] { dicomFile1, dicomFile2 });

            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Fact]
        public async Task GivenMultipleInstancesWithMixTransferSyntax_WhenRetrieveSeriesWithOriginalTransferSyntax_ThenServerShouldReturnOrignialContents()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                seriesInstanceUid,
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await InternalStoreAsync(new[] { dicomFile1, dicomFile2 });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax: "*");

            DicomFile[] instancesInStudy = await response.ToArrayAsync();

            Assert.Equal(2, instancesInStudy.Length);

            byte[][] actual = instancesInStudy.Select(item => item.ToByteArray()).ToArray();

            Assert.Contains(dicomFile1.ToByteArray(), actual);
            Assert.Contains(dicomFile2.ToByteArray(), actual);
        }

        [Fact]
        public async Task GivenNonExistingSeries_WhenRetrieveSeries_ThenServerShouldReturnNotFound()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveSeriesAsync(TestUidGenerator.Generate(), TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenInstanceWithoutPixelData_WhenRetrieveSeries_ThenServerShouldReturnExpectedContent()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var seriesInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid, seriesInstanceUid);
            await InternalStoreAsync(new[] { dicomFile1 });

            using DicomWebAsyncEnumerableResponse<DicomFile> response = await _client.RetrieveSeriesAsync(studyInstanceUid, seriesInstanceUid, dicomTransferSyntax: "*");

            DicomFile[] studyRetrieve = await response.ToArrayAsync();

            Assert.Equal(
                new[] { dicomFile1.ToByteArray() },
                studyRetrieve.Select(item => item.ToByteArray()));
        }
    }
}
