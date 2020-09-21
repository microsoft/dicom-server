// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class RetrieveResourcesAcceptanceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly IDicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private static readonly CancellationToken _defaultCancellationToken = new CancellationTokenSource().Token;

        public RetrieveResourcesAcceptanceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

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
        public async Task GivenInputAndOutputTransferSyntax_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = await DicomFile.OpenAsync(transcoderTestData.InputDicomFile);
            int numberOfFrames = DicomPixelData.Create(inputDicomFile.Dataset).NumberOfFrames;

            string studyInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);
            string seriesInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID);
            string sopInstanceUid = inputDicomFile.Dataset.GetString(DicomTag.SOPInstanceUID);

            DicomWebResponse<IEnumerable<DicomDataset>> tryQuery = await _client.QueryAsync(
                   $"/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances?SOPInstanceUID={sopInstanceUid}");

            if (tryQuery.StatusCode == HttpStatusCode.OK)
            {
                await _client.DeleteStudyAsync(studyInstanceUid);
            }

            await _client.StoreAsync(new[] { inputDicomFile });

            DicomWebResponse<IReadOnlyList<Stream>> response = await _client.RetrieveFramesAsync(
                  studyInstanceUid: studyInstanceUid,
                  seriesInstanceUid: seriesInstanceUid,
                  sopInstanceUid: sopInstanceUid,
                  mediaType: mediaType,
                  dicomTransferSyntax: transferSyntax,
                  frames: GenerateFrames(numberOfFrames),
                  cancellationToken: _defaultCancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string byteStreamHash = TranscoderTestDataHelper.GetHashFromStream(response.Value[0]);
            Assert.Equal(transcoderTestData.MetaData.OutputFramesHashCode, byteStreamHash);

            await _client.DeleteStudyAsync(studyInstanceUid);
        }

        private static int[] GenerateFrames(int numberOfFrames)
        {
            return Enumerable.Range(1, numberOfFrames).ToArray();
        }
    }
}
