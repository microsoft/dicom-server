// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Dicom;
using Dicom.Imaging;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Retrieve
{
    public class TranscoderTests
    {
        private const string TestFileFolder = "TranscoderTestsFiles";
        private const string AllFiles = "*";
        private const string ExpectedOutputFileName = "ExpectedOutput.dcm";
        private const string MetadataFileName = "Metadata.json";
        private const string InputFileName = "Input.dcm";
        private const string DecodeTestFolder = TestFileFolder + @"\Decode";
        private const string EncodeTestFolder = TestFileFolder + @"\Encode";
        private const string UncompressedTestFolder = TestFileFolder + @"\Uncompressed";
        private ITranscoder _transcoder;
        private RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public TranscoderTests()
        {
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _transcoder = new Transcoder(_recyclableMemoryStreamManager);
        }

        [Theory(Skip = "Skip now until https://microsofthealth.visualstudio.com/Health/_workitems/edit/75149 is resolved")]
        [MemberData(nameof(GetTestData), EncodeTestFolder)]
        public async void GivenUncompressedTransferSyntax_WhenRequestEncoding_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        [Theory(Skip = "Skip now until https://microsofthealth.visualstudio.com/Health/_workitems/edit/75149 is resolved")]
        [MemberData(nameof(GetTestData), DecodeTestFolder)]
        public async void GivenCompressedTranserSyntax_WhenRequestDecoding_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        [Theory(Skip = "Skip now until https://microsofthealth.visualstudio.com/Health/_workitems/edit/75149 is resolved")]
        [MemberData(nameof(GetTestData), UncompressedTestFolder)]
        public async void GivenUncompressedTransferSytnax_WhenRequestAnotherUncompressedTransferSyntax_ThenTranscodingShouldSucceed(TranscoderTestData testData)
        {
            await VerifyTranscoding(testData);
        }

        private static string GetExpectedFile(string inputFile)
        {
            return Path.Combine(Path.GetDirectoryName(inputFile), ExpectedOutputFileName);
        }

        private static string GetMetadataFile(string inputFile)
        {
            return Path.Combine(Path.GetDirectoryName(inputFile), MetadataFileName);
        }

        private static bool IsInputFile(string path)
        {
            return Path.GetFileName(path).Equals(InputFileName, StringComparison.InvariantCultureIgnoreCase);
        }

        private static TranscoderTestMetadata GetMetadata(string inputFile)
        {
            string metadataFile = GetMetadataFile(inputFile);
            return JsonSerializer.Deserialize<TranscoderTestMetadata>(File.ReadAllText(metadataFile));
        }

        public static IEnumerable<object[]> GetTestData(string folder)
        {
            IList<object[]> result = new List<object[]>();
            foreach (string path in Directory.EnumerateFiles(folder, AllFiles, SearchOption.AllDirectories))
            {
                if (IsInputFile(path))
                {
                    TranscoderTestData testData = new TranscoderTestData()
                    {
                        InputDicomFile = path,
                        ExpectedOutputDicomFile = GetExpectedFile(path),
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
            Assert.Equal(inputFile.Dataset.InternalTransferSyntax, testData.MetaData.GetInputSyntax());

            // Set stream position to begin for trasncoder to consume
            fileStream.Seek(0, SeekOrigin.Begin);

            Stream outputFileStream = await _transcoder.TranscodeFileAsync(fileStream, testData.MetaData.GetOutputSyntax().UID.UID);
            DicomFile outputFile = await DicomFile.OpenAsync(outputFileStream);

            Assert.Equal(outputFile.Dataset.InternalTransferSyntax, testData.MetaData.GetOutputSyntax());

            VerifyFrames(outputFile, testData);
            return outputFile;
        }

        private void VerifyFrames(DicomFile actual, TranscoderTestData testData)
        {
            Assert.Equal(actual.Dataset.InternalTransferSyntax, testData.MetaData.GetOutputSyntax());
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
