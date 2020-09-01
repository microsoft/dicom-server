// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class TranscoderTests
    {
        private const string TestFileFolder = "TranscoderTestsFiles";
        private const string TestFileFolderForDecode = TestFileFolder + @"\Decode";
        private const string TestFileFolderForEncode = TestFileFolder + @"\Encode";
        private const string TestFileFolderForUncompressed = TestFileFolder + @"\Uncompressed";
        private ITranscoder _transcoder;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public TranscoderTests()
        {
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _transcoder = new Transcoder(_recyclableMemoryStreamManager);
        }

        [Theory(Skip = "fodicom bug https://github.com/fo-dicom/fo-dicom/issues/1099 ([.NetCore]Encoding to JPEG2000Lossless is not handled correctly")]
        [MemberData(nameof(GetTestDatas), TestFileFolderForEncode)]
        public async Task GivenUncompressedDicomFile_WhenRequestEncoding_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        [Theory]
        [MemberData(nameof(GetTestDatas), TestFileFolderForDecode)]
        public async Task GivenCompressedDicomFile_WhenRequestDecoding_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        [Theory]
        [MemberData(nameof(GetTestDatas), TestFileFolderForUncompressed)]
        public async Task GivenUncompressedDicomFile_WhenRequestAnotherUncompressedTransferSyntax_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        public static IEnumerable<object[]> GetTestDatas(string testFolder)
        {
            return TranscoderTestDataHelper.GetTestDatas(testFolder)
                .Select(item => new object[] { item });
        }

        private async Task<DicomFile> VerifyTranscoding(TranscoderTestData testData)
        {
            Stream fileStream = File.OpenRead(testData.InputDicomFile);
            DicomFile inputFile = DicomFile.Open(fileStream);

            // Verify if input file has correct input transfersyntax
            Assert.Equal(inputFile.Dataset.InternalTransferSyntax.UID.UID, testData.MetaData.InputSyntaxUid);

            // Reset stream position for trasncoder to consume
            fileStream.Seek(0, SeekOrigin.Begin);

            DicomFile outputFile = await VerifyTranscodeFileAsync(testData, fileStream, inputFile);
            VerifyTranscodeFrame(testData, inputFile);
            return outputFile;
        }

        private void VerifyTranscodeFrame(TranscoderTestData testData, DicomFile inputFile)
        {
            Stream result = _transcoder.TranscodeFrame(inputFile, 0, testData.MetaData.OutputSyntaxUid);
            string hashcode = GetByteArrayHashCode(ToByteArray(result));
            Assert.Equal(testData.MetaData.Frame0HashCode, hashcode);
        }

        private async Task<DicomFile> VerifyTranscodeFileAsync(TranscoderTestData testData, Stream fileStream, DicomFile inputFile)
        {
            Stream outputFileStream = await _transcoder.TranscodeFileAsync(fileStream, testData.MetaData.OutputSyntaxUid);
            DicomFile outputFile = await DicomFile.OpenAsync(outputFileStream);

            Assert.Equal(outputFile.Dataset.InternalTransferSyntax.UID.UID, testData.MetaData.OutputSyntaxUid);

            // Verify file metainfo
            VerifyDicomItems(inputFile.FileMetaInfo, outputFile.FileMetaInfo, DicomTag.FileMetaInformationGroupLength, DicomTag.TransferSyntaxUID);

            // Verify dataset
            VerifyDicomItems(inputFile.Dataset, outputFile.Dataset, DicomTag.PixelData, DicomTag.PhotometricInterpretation);

            VerifyFrames(outputFile, testData);
            return outputFile;
        }

        private byte[] ToByteArray(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private void VerifyDicomItems(IEnumerable<DicomItem> expected, IEnumerable<DicomItem> actual, params DicomTag[] ignoredTags)
        {
            ISet<DicomTag> ignoredSet = new HashSet<DicomTag>(ignoredTags);
            Dictionary<DicomTag, DicomItem> expectedDict = expected.ToDictionary(item => item.Tag);
            Dictionary<DicomTag, DicomItem> actualDict = actual.ToDictionary(item => item.Tag);
            Assert.Equal(expectedDict.Count, actualDict.Count);
            foreach (DicomTag tag in expectedDict.Keys)
            {
                if (ignoredSet.Contains(tag))
                {
                    continue;
                }

                Assert.True(actualDict.ContainsKey(tag));
                Assert.Equal(expectedDict[tag], actualDict[tag], new DicomItemEqualityComparer());
            }
        }

        private void VerifyFrames(DicomFile actual, TranscoderTestData testData)
        {
            Assert.Equal(actual.Dataset.InternalTransferSyntax.UID.UID, testData.MetaData.OutputSyntaxUid);
            Assert.Equal(testData.MetaData.OutputFramesHashCode, GetFramesHashCode(actual));
        }

        private string GetFramesHashCode(DicomFile dicomFile)
        {
            DicomPixelData dicomPixelData = DicomPixelData.Create(dicomFile.Dataset);
            List<byte> frames = new List<byte>();
            for (int i = 0; i < dicomPixelData.NumberOfFrames; i++)
            {
                frames.AddRange(dicomPixelData.GetFrame(i).Data);
            }

            return GetByteArrayHashCode(frames.ToArray());
        }

        private string GetByteArrayHashCode(byte[] byteArray)
        {
            return Convert.ToBase64String(new SHA1Managed().ComputeHash(byteArray));
        }
    }
}
