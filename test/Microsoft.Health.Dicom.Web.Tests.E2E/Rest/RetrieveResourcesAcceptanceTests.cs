// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
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

        [Theory]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\ContentTypeIsApplicationOctetStream", DicomWebConstants.ApplicationOctetStreamMeidaType, null)]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\ContentTypeIsApplicationOctetStream", DicomWebConstants.ApplicationOctetStreamMeidaType, "1.2.840.10008.1.2.1")]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\ContentTypeIsApplicationOctetStream", DicomWebConstants.ApplicationOctetStreamMeidaType, "*")]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\ContentTypeIsImageJp2", DicomWebConstants.ImageJpeg2000MeidaType, null)]
        [InlineData(@"TranscoderTestsFiles\EndToEnd\GetFrame\ContentTypeIsImageJp2", DicomWebConstants.ImageJpeg2000MeidaType, "1.2.840.10008.1.2.4.90")]
        public async Task GivenInputAndOutputTransferSyntax_WhenRetrieveFrame_ThenServerShouldReturnExpectedContent(string testDataFolder, string mediaType, string transferSyntax)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = await DicomFile.OpenAsync(transcoderTestData.InputDicomFile);
            int numberOfFrames = DicomPixelData.Create(inputDicomFile.Dataset).NumberOfFrames;

            // upload file (TODO: check existing before upload)
            try
            {
                var result = await _client.StoreAsync(new[] { inputDicomFile });
            }
            catch (DicomWebException)
            {
            }

            // retrieve
            DicomWebResponse<IReadOnlyList<Stream>> response = await _client.RetrieveFramesAsync(
                  studyInstanceUid: inputDicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                  seriesInstanceUid: inputDicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                  sopInstanceUid: inputDicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
                  mediaType: mediaType,
                  dicomTransferSyntax: transferSyntax,
                  frames: GenerateFrames(numberOfFrames),
                  cancellationToken: _defaultCancellationToken);

            // Verify StatusCode
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // TODO: Verify response content type (can we do this?)

            // TODO:  Verify response content
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
