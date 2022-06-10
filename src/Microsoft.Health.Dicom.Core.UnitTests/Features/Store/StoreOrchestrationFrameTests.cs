// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Store;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Store;
public class StoreOrchestrationFrameTests
{
    [Fact]
    public void GivenDicom_WithNoImage_ReturnsNull()
    {
        // arrange
        var ds = new DicomDataset
            {
                { DicomTag.SOPInstanceUID, DicomUID.Generate() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
                { DicomTag.ImageComments, " ".PadLeft(8000) }
            };

        // act
        Dictionary<int, FrameRange> ranges = StoreOrchestrator.GetFramesOffset(ds);

        // assert
        Assert.Null(ranges);
    }

    [Fact]
    public void GivenDicom_WithFragmentFrame_ReturnsFrameRange()
    {
        // arrange
        using Stream stream = new MemoryStream(Resource.layer1);
        DicomFile dicomFile = DicomFile.Open(stream, FileReadOption.ReadLargeOnDemand, largeObjectSize: 1000);

        // act
        Dictionary<int, FrameRange> ranges = StoreOrchestrator.GetFramesOffset(dicomFile.Dataset);

        // assert
        ValidateOffsetParser(ranges, dicomFile, Resource.layer1);
    }

    [Fact]
    public void GivenDicom_WithOtherByteFrame_ReturnsFrameRange()
    {
        // arrange
        using Stream stream = new MemoryStream(Resource.red_triangle);
        DicomFile dicomFile = DicomFile.Open(stream, FileReadOption.ReadLargeOnDemand, largeObjectSize: 1000);

        // act
        Dictionary<int, FrameRange> ranges = StoreOrchestrator.GetFramesOffset(dicomFile.Dataset);

        // assert
        ValidateOffsetParser(ranges, dicomFile, Resource.red_triangle);
    }

    [Fact]
    public void GivenDicom_WithOtherWordFrame_ReturnsFrameRange()
    {
        // arrange
        using Stream stream = new MemoryStream(Resource.case1_008);
        DicomFile dicomFile = DicomFile.Open(stream, FileReadOption.ReadLargeOnDemand, largeObjectSize: 1000);

        // act
        Dictionary<int, FrameRange> ranges = StoreOrchestrator.GetFramesOffset(dicomFile.Dataset);

        // assert
        ValidateOffsetParser(ranges, dicomFile, Resource.case1_008);
    }

    private static void ValidateOffsetParser(Dictionary<int, FrameRange> ranges, DicomFile file, byte[] originalStream)
    {
        foreach (var range in ranges)
        {
            DicomPixelData dicomPixelData = DicomPixelData.Create(file.Dataset);
            var ebyteBuffer = dicomPixelData.GetFrame(range.Key);
            byte[] abyteBuffer = originalStream.Skip((int)range.Value.Offset).Take((int)range.Value.Length).ToArray();
            Assert.True(ValidateStreamContent(abyteBuffer, ebyteBuffer.Data));
        }
    }

    private static bool ValidateStreamContent(byte[] actual, byte[] expected)
    {
        if (actual.Length != expected.Length)
        {
            return false;
        }
        return actual.SequenceEqual(expected);
    }
}
