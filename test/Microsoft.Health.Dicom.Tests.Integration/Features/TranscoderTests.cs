// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
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
        private const string AllFiles = "*";
        private const string ExpectedOutputFileName = "ExpectedOutput.dcm";
        private const string MetadataFileName = "Metadata.json";
        private const string InputFileName = "Input.dcm";
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

        [Theory]
        [MemberData(nameof(GetTestDatas), TestFileFolderForEncode, Skip = "fodicom bug https://github.com/fo-dicom/fo-dicom/issues/1099 ([.NetCore]Encoding to JPEG2000Lossless is not handled correctly)")]
        public async void GivenUncompressedDicomFile_WhenRequestEncoding_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        [Theory]
        [MemberData(nameof(GetTestDatas), TestFileFolderForDecode)]
        public async void GivenCompressedDicomFile_WhenRequestDecoding_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        [Theory]
        [MemberData(nameof(GetTestDatas), TestFileFolderForUncompressed)]
        public async void GivenUncompressedDicomFile_WhenRequestAnotherUncompressedTransferSyntax_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        private static string GetExpectedOutputFile(string inputFile)
        {
            return Path.Combine(Path.GetDirectoryName(inputFile), ExpectedOutputFileName);
        }

        private static string GetMetadataFile(string inputFile)
        {
            return Path.Combine(Path.GetDirectoryName(inputFile), MetadataFileName);
        }

        private static bool IsInputFile(string path)
        {
            return Path.GetFileName(path).Equals(InputFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static TranscoderTestMetadata GetMetadata(string inputFile)
        {
            string metadataFile = GetMetadataFile(inputFile);
            return JsonSerializer.Deserialize<TranscoderTestMetadata>(File.ReadAllText(metadataFile));
        }

        public static IEnumerable<object[]> GetTestDatas(string testFileFolder)
        {
            IList<object[]> result = new List<object[]>();
            foreach (string path in Directory.EnumerateFiles(testFileFolder, AllFiles, SearchOption.AllDirectories))
            {
                if (IsInputFile(path))
                {
                    TranscoderTestData testData = new TranscoderTestData()
                    {
                        InputDicomFile = path,
                        ExpectedOutputDicomFile = GetExpectedOutputFile(path),
                        MetaData = GetMetadata(path),
                    };
                    result.Add(new object[] { testData });
                }
            }

            return result;
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
