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
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.NativeCodec;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Features.FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using Microsoft.Health.Dicom.Tests.Common.TranscoderTests;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Integration.Features;

public class TranscoderTests
{
    private const string TestDataRootFolder = "TranscoderTestsFiles";
    private const string DecodeFolder = "Decode";
    private const string EncodeFolder = "Encode";
    private const string UncompressedFolder = "Uncompressed";
    private readonly ITranscoder _transcoder;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public TranscoderTests()
    {
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        _transcoder = new Transcoder(_recyclableMemoryStreamManager, NullLogger<Transcoder>.Instance);

        CustomDicomImplementation.SetDicomImplementationClassUIDAndVersion();
        new DicomSetupBuilder()
            .RegisterServices(s => s.AddTranscoderManager<NativeTranscoderManager>())
            .Build();
    }

    [Theory]
    [MemberData(nameof(GetTestDatas), EncodeFolder)]
    public async Task GivenUncompressedDicomFile_WhenRequestEncoding_ThenTranscodingShouldSucceed(string testDataFolder)
    {
        await VerifyTranscoding(testDataFolder);
    }

    [Theory]
    [MemberData(nameof(GetTestDatas), DecodeFolder)]
    public async Task GivenCompressedDicomFile_WhenRequestDecoding_ThenTranscodingShouldSucceed(string testDataFolder)
    {
        await VerifyTranscoding(testDataFolder);
    }

    [Theory]
    [MemberData(nameof(GetTestDatas), UncompressedFolder)]
    public async Task GivenUncompressedDicomFile_WhenRequestAnotherUncompressedTransferSyntax_ThenTranscodingShouldSucceed(string testDataFolder)
    {
        await VerifyTranscoding(testDataFolder);
    }

    public static IEnumerable<object[]> GetTestDatas(string subFolder)
        => TranscoderTestDataHelper.GetTestDataFolders(Path.Combine(TestDataRootFolder, subFolder)).Select(item => new object[] { item });

    private async Task<DicomFile> VerifyTranscoding(string testDataFolder)
    {
        TranscoderTestData testData = TranscoderTestDataHelper.GetTestData(testDataFolder);
        using (FileStream fileStream = File.OpenRead(testData.InputDicomFile))
        {
            DicomFile inputFile = DicomFile.Open(fileStream);

            // Verify if input file has correct input transfersyntax
            Assert.Equal(inputFile.Dataset.InternalTransferSyntax.UID.UID, testData.MetaData.InputSyntaxUid);

            // Reset stream position for trasncoder to consume
            fileStream.Seek(0, SeekOrigin.Begin);

            DicomFile outputFile = await VerifyTranscodeFileAsync(testData, fileStream, inputFile);
            VerifyTranscodeFrame(testData, inputFile);
            return outputFile;
        }
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
        VerifyDicomItems(inputFile.FileMetaInfo, outputFile.FileMetaInfo, DicomTag.FileMetaInformationGroupLength, DicomTag.TransferSyntaxUID, DicomTag.ImplementationVersionName, DicomTag.ImplementationClassUID);

        // Verify dataset
        VerifyDicomItems(inputFile.Dataset, outputFile.Dataset, DicomTag.PixelData, DicomTag.PhotometricInterpretation, DicomTag.ImplementationVersionName, DicomTag.ImplementationClassUID);

        VerifyFrames(outputFile, testData);

        VerifyImplementationClassUID(outputFile);

        return outputFile;
    }

    private static byte[] ToByteArray(Stream stream)
    {
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private static void VerifyDicomItems(IEnumerable<DicomItem> expected, IEnumerable<DicomItem> actual, params DicomTag[] ignoredTags)
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

    private static void VerifyFrames(DicomFile actual, TranscoderTestData testData)
    {
        Assert.Equal(actual.Dataset.InternalTransferSyntax.UID.UID, testData.MetaData.OutputSyntaxUid);
        Assert.Equal(testData.MetaData.OutputFramesHashCode, GetFramesHashCode(actual));
    }

    private static void VerifyImplementationClassUID(DicomFile actual)
    {
        (DicomUID expectedUID, string expectedVersion) = Samples.GetDicomImplemenationClasUIDAndVersionName();

        Assert.Equal(expectedUID, actual.FileMetaInfo.ImplementationClassUID);
        Assert.Equal(expectedVersion, actual.FileMetaInfo.ImplementationVersionName);
    }

    private static string GetFramesHashCode(DicomFile dicomFile)
    {
        DicomPixelData dicomPixelData = DicomPixelData.Create(dicomFile.Dataset);
        List<byte> frames = new List<byte>();
        for (int i = 0; i < dicomPixelData.NumberOfFrames; i++)
        {
            frames.AddRange(dicomPixelData.GetFrame(i).Data);
        }

        return GetByteArrayHashCode(frames.ToArray());
    }

    private static string GetByteArrayHashCode(byte[] byteArray)
    {
        return Convert.ToBase64String(SHA1.HashData(byteArray));
    }
}
