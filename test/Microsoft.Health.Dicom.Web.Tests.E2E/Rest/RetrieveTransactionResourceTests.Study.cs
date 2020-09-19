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
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client;
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
        public async Task GivenSupportedAcceptHeaders_WhenRetrieveStudy_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = DicomFile.Open(transcoderTestData.InputDicomFile);
            await EnsureFileIsStoredAsync(inputDicomFile);
            string studyInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseStudyUriFormat, studyInstanceUid), UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: false, mediaType, transferSyntax);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
            {
                Assert.True(response.IsSuccessStatusCode);

                // read streams
                Dictionary<MultipartSection, Stream> sections = await response.Content.ReadAsMultipartDictionaryAsync();
                foreach (var item in sections.Keys)
                {
                    Assert.Equal(mediaType, item.ContentType);
                    DicomFile actual = DicomFile.Open(sections[item]);
                    DicomFile expected = DicomFile.Open(transcoderTestData.ExpectedOutputDicomFile);
                    Assert.Equal(expected, actual, new DicomFileEqualityComparer());
                }
            }
        }

        [Theory]
        [InlineData(true, DicomWebConstants.ApplicationDicomMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // use single part instead of multiple part
        [InlineData(false, DicomWebConstants.ApplicationOctetStreamMediaType, DicomWebConstants.OriginalDicomTransferSyntax)] // unsupported media type image/png
        [InlineData(false, DicomWebConstants.ApplicationDicomMediaType, "1.2.840.10008.1.2.4.100")] // unsupported media type MPEG2
        public async Task GivenUnsupportedAcceptHeaders_WhenRetrieveStudy_ThenServerShouldReturnNotAcceptable(bool singlePart, string mediaType, string transferSyntax)
        {
            var requestUri = new Uri(string.Format(DicomWebConstants.BaseStudyUriFormat, TestUidGenerator.Generate()), UriKind.Relative);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessageBuilder().Build(requestUri, singlePart: singlePart, mediaType, transferSyntax);

            using (HttpResponseMessage response = await _client.HttpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource().Token))
            {
                Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
            }
        }

        [Fact]
        public async Task GivenMultipleInstances_WhenRetrieveStudy_ThenServerShouldReturnExpectedInstances()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            var studyInstanceUid2 = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile3 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid2,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            await InternalStoreAsync(new[] { dicomFile1, dicomFile2, dicomFile3 });

            DicomWebResponse<IReadOnlyList<DicomFile>> instancesInStudy = await _client.RetrieveStudyAsync(studyInstanceUid);
            Assert.Equal(2, instancesInStudy.Value.Count);
            var actual = instancesInStudy.Value.Select(item => item.ToByteArray());
            Assert.Contains(dicomFile1.ToByteArray(), actual);
            Assert.Contains(dicomFile2.ToByteArray(), actual);
        }

        [Fact]
        public async Task GivenMultipleInstancesWithMixTransferSyntax_WhenRetrieveStudy_ThenServerShouldReturnNotAcceptable()
        {
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await InternalStoreAsync(new[] { dicomFile1, dicomFile2 });
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyAsync(studyInstanceUid, dicomTransferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID));
            Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
        }

        [Fact]
        public async Task GivenMultipleInstancesWithMixTransferSyntax_WhenRetrieveStudyWithOriginalTransferSyntax_ThenServerShouldReturnOrignialContents()
        {
            var studyInstanceUid = TestUidGenerator.Generate();

            DicomFile dicomFile1 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.ExplicitVRLittleEndian.UID.UID);

            DicomFile dicomFile2 = Samples.CreateRandomDicomFileWith8BitPixelData(
                studyInstanceUid,
                TestUidGenerator.Generate(),
                transferSyntax: DicomTransferSyntax.MPEG2.UID.UID,
                encode: false);

            await InternalStoreAsync(new[] { dicomFile1, dicomFile2 });
            DicomWebResponse<IReadOnlyList<DicomFile>> instancesInStudy = await _client.RetrieveStudyAsync(studyInstanceUid, dicomTransferSyntax: "*");
            Assert.Equal(2, instancesInStudy.Value.Count);
            var actual = instancesInStudy.Value.Select(item => item.ToByteArray());
            Assert.Contains(dicomFile1.ToByteArray(), actual);
            Assert.Contains(dicomFile2.ToByteArray(), actual);
        }

        [Fact]
        public async Task GivenNonExistingStudy_WhenRetrieveStudy_ThenServerShouldReturnNotFound()
        {
            DicomWebException exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.RetrieveStudyAsync(TestUidGenerator.Generate()));
            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public async Task GivenInstanceWithoutPixelData_WhenRetrieveStudy_ThenThenServerShouldReturnExpectedContent()
        {
            var studyInstanceUid = TestUidGenerator.Generate();
            DicomFile dicomFile1 = Samples.CreateRandomDicomFile(studyInstanceUid);
            await InternalStoreAsync(new[] { dicomFile1 });

            DicomWebResponse<IReadOnlyList<DicomFile>> studyRetrieve = await _client.RetrieveStudyAsync(studyInstanceUid, dicomTransferSyntax: "*");

            Assert.Equal(dicomFile1.ToByteArray(), studyRetrieve.Value[0].ToByteArray());
        }
    }
}
