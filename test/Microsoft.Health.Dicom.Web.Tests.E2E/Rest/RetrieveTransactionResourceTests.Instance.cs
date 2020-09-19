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
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Microsoft.Health.Dicom.Web.Tests.E2E.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public partial class RetrieveTransactionResourceTests
    {
        [Theory]
        [InlineData(RequestOriginalContentTestFolder, DicomWebConstants.ApplicationDicomMediaType, "*")]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, DicomWebConstants.ApplicationDicomMediaType, null)]
        [InlineData(FromJPEG2000LosslessToExplicitVRLittleEndianTestFolder, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.1")]
        [InlineData(FromExplicitVRLittleEndianToJPEG2000LosslessTestFolder, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.90")]
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveInstance_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            await EnsureFileIsStoredAsync(inputDicomFile);
            var instanceId = inputDicomFile.Dataset.ToInstanceIdentifier();
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseInstanceUriFormat, instanceId.StudyInstanceUid, instanceId.SeriesInstanceUid, instanceId.SopInstanceUid), UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: true, mediaType, transferSyntax);
            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
            {
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(mediaType, response.Content.Headers.ContentType.MediaType);

                var stream = await response.Content.ReadyAsSeekableStreamAsync();
                DicomFile actual = DicomFile.Open(stream);
                DicomFile expected = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
                Assert.Equal(expected, actual, new DicomFileEqualityComparer());
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

            await InternalStoreAsync(new[] { dicomFile });
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
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

            await InternalStoreAsync(new[] { dicomFile });
            DicomWebResponse<IReadOnlyList<DicomFile>> instancesInStudy = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");
            Assert.Equal(1, instancesInStudy.Value.Count);
            Assert.Equal(dicomFile.ToByteArray(), instancesInStudy.Value[0].ToByteArray());
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

            DicomWebResponse<IReadOnlyList<DicomFile>> instanceRetrieve = await _client.RetrieveInstanceAsync(studyInstanceUid, seriesInstanceUid, sopInstanceUid, dicomTransferSyntax: "*");

            Assert.Equal(dicomFile1.ToByteArray(), instanceRetrieve.Value[0].ToByteArray());
        }
    }
}
