// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
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

        [Theory]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\FromJPEG2ToOctet", DicomWebConstants.ApplicationOctetStreamMeidaType, null)]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\FromJPEG2ToOctet", DicomWebConstants.ApplicationOctetStreamMeidaType, "1.2.840.10008.1.2.1")]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\FromJPEG2ToJPEG2", DicomWebConstants.ApplicationOctetStreamMeidaType, "*")]
        public async Task GivenInputAndOutputTransferSyntax_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            /* TODO: Add in following test cases after Octet ot JPEG2 transcoder working
            [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\FromOctetToJPEG2", DicomWebConstants.ImageJpeg2000MeidaType, null)]
            [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\FromOctetToJPEG2", DicomWebConstants.ImageJpeg2000MeidaType, "1.2.840.10008.1.2.4.90")]
            */

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

            string byteStreamHash = GetStreamHashCode(response.Value[0]);
            Assert.Equal(transcoderTestData.MetaData.OutputFramesHashCode, byteStreamHash);

            await _client.DeleteStudyAsync(studyInstanceUid);
        }

        private string GetStreamHashCode(Stream byteStream)
        {
            return Convert.ToBase64String(new SHA1Managed().ComputeHash(byteStream));
        }

        private static int[] GenerateFrames(int numberOfFrames)
        {
            List<int> result = new List<int>();
            for (int i = 1; i <= numberOfFrames; i++)
            {
                result.Add(i);
            }

            return result.ToArray();
        }
    }
}
