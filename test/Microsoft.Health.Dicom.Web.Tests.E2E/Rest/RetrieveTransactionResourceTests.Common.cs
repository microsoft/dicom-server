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
using Dicom.Imaging;
using Dicom.IO.Buffer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Xunit;
using MediaTypeHeaderValue = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    /// <summary>
    /// The tests for retrieving resources.
    /// </summary>
    public partial class RetrieveTransactionResourceTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private const string TransferSyntaxHeaderName = "transfer-syntax";
        private const string MultipartRelatedContentType = "multipart/related";

        private readonly IDicomWebClient _client;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private static readonly CancellationToken _defaultCancellationToken = new CancellationTokenSource().Token;

        public static readonly List<string> SupportedTransferSyntaxesFor8BitTranscoding = new List<string>
        {
            "DeflatedExplicitVRLittleEndian",
            "ExplicitVRBigEndian",
            "ExplicitVRLittleEndian",
            "ImplicitVRLittleEndian",
            "JPEG2000Lossless",
            "JPEG2000Lossy",
            "JPEGProcess1",
            "JPEGProcess2_4",
            "RLELossless",
        };

        public static readonly List<string> SupportedTransferSyntaxesForOver8BitTranscoding = new List<string>
        {
            "DeflatedExplicitVRLittleEndian",
            "ExplicitVRBigEndian",
            "ExplicitVRLittleEndian",
            "ImplicitVRLittleEndian",
            "RLELossless",
        };

        public RetrieveTransactionResourceTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.Client;
            _recyclableMemoryStreamManager = fixture.RecyclableMemoryStreamManager;
        }

        public static IEnumerable<object[]> GetInvalidTransferSyntaxData()
        {
            yield return new object[] { DicomTransferSyntax.ExplicitVRLittleEndian.ToString() };
            yield return new object[] { "unknown" };
            yield return new object[] { "&&5" };
        }

        public static IEnumerable<object[]> Get8BitTranscoderCombos()
        {
            List<string> fromList = SupportedTransferSyntaxesFor8BitTranscoding;
            List<string> toList = SupportedTransferSyntaxesFor8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        public static IEnumerable<object[]> Get16BitTranscoderCombos()
        {
            List<string> fromList = SupportedTransferSyntaxesForOver8BitTranscoding;
            List<string> toList = SupportedTransferSyntaxesForOver8BitTranscoding;

            return from x in fromList from y in toList select new[] { x, y };
        }

        private async Task<(InstanceIdentifier, DicomFile)> CreateAndStoreDicomFile(int numberOfFrames = 0)
        {
            DicomFile dicomFile = Samples.CreateRandomDicomFileWithPixelData(frames: numberOfFrames);
            var dicomInstance = dicomFile.Dataset.ToInstanceIdentifier();
            await _client.StoreAsync(new[] { dicomFile }, dicomInstance.StudyInstanceUid);

            return (dicomInstance, dicomFile);
        }

        private void ValidateRetrieveTransaction(
            DicomWebResponse<IReadOnlyList<DicomFile>> response,
            HttpStatusCode expectedStatusCode,
            DicomTransferSyntax expectedTransferSyntax,
            bool singleInstance = false,
            params DicomFile[] expectedFiles)
        {
            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedFiles.Length, response.Value.Count);

            if (singleInstance)
            {
                Assert.Equal(KnownContentTypes.ApplicationDicom, response.Content.Headers.ContentType.MediaType);
            }
            else
            {
                Assert.Equal(KnownContentTypes.MultipartRelated, response.Content.Headers.ContentType.MediaType);
            }

            for (var i = 0; i < expectedFiles.Length; i++)
            {
                DicomFile expectedFile = expectedFiles[i];
                var expectedInstance = expectedFile.Dataset.ToInstanceIdentifier();
                DicomFile actualFile = response.Value.First(x => x.Dataset.ToInstanceIdentifier().Equals(expectedInstance));

                Assert.Equal(expectedTransferSyntax, response.Value[i].Dataset.InternalTransferSyntax);

                // If the same transfer syntax as original, the files should be exactly the same
                if (expectedFile.Dataset.InternalTransferSyntax == actualFile.Dataset.InternalTransferSyntax)
                {
                    var expectedFileArray = DicomFileToByteArray(expectedFile);
                    var actualFileArray = DicomFileToByteArray(actualFile);

                    Assert.Equal(expectedFileArray.Length, actualFileArray.Length);

                    for (var ii = 0; ii < expectedFileArray.Length; ii++)
                    {
                        Assert.Equal(expectedFileArray[ii], actualFileArray[ii]);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private byte[] DicomFileToByteArray(DicomFile dicomFile)
        {
            using (MemoryStream memoryStream = _recyclableMemoryStreamManager.GetStream())
            {
                dicomFile.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static void AssertPixelDataEqual(IByteBuffer expectedPixelData, Stream actualPixelData)
        {
            Assert.Equal(expectedPixelData.Size, actualPixelData.Length);
            Assert.Equal(0, actualPixelData.Position);
            for (var i = 0; i < expectedPixelData.Size; i++)
            {
                Assert.Equal(expectedPixelData.Data[i], actualPixelData.ReadByte());
            }
        }

        private static int[] GenerateFrames(int numberOfFrames)
        {
            return Enumerable.Range(1, numberOfFrames).ToArray();
        }

        private async Task UploadTestData(string testDataFolder)
        {
            TranscoderTestData transcoderTestData = TranscoderTestDataHelper.GetTestData(testDataFolder);
            DicomFile inputDicomFile = await DicomFile.OpenAsync(transcoderTestData.InputDicomFile);

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
        }

        private static async Task<Dictionary<MultipartSection, Stream>> ReadMultipart(HttpContent content, CancellationToken cancellationToken)
        {
            Dictionary<MultipartSection, Stream> result = new Dictionary<MultipartSection, Stream>();
            await using (Stream stream = await content.ReadAsStreamAsync())
            {
                MultipartSection part;
                var media = MediaTypeHeaderValue.Parse(content.Headers.ContentType.ToString());
                var multipartReader = new MultipartReader(HeaderUtilities.RemoveQuotes(media.Boundary).Value, stream, 100);

                while ((part = await multipartReader.ReadNextSectionAsync(cancellationToken)) != null)
                {
                    MemoryStream memStream = new MemoryStream();
                    await part.Body.CopyToAsync(memStream, cancellationToken);
                    memStream.Seek(0, SeekOrigin.Begin);
                    result.Add(part, memStream);
                }
            }

            return result;
        }

        private static void VerifyFrameAreEquals(Stream actual, DicomFile expected, int frameIndex)
        {
            DicomPixelData pixelData = DicomPixelData.Create(expected.Dataset);
            byte[] expectedData = pixelData.GetFrame(frameIndex).Data;
            byte[] actualData = ToByteArray(actual);
            Assert.Equal(expectedData, actualData);
        }

        private static byte[] ToByteArray(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
