// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Xunit;
using DicomFileExtensions = Microsoft.Health.Dicom.Core.Features.Retrieve.DicomFileExtensions;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions;
public class DicomFileExtensionsTests
{
    [Fact]
    public async Task GivenValidInput_WhenGetDatasetLengthAsyncIsCalled_ReturnsCorrectLength()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() },
            { DicomTag.PatientName, "Test^Patient" }
        };
        var dcmFile = new DicomFile(dataset);

        var recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        var length = await DicomFileExtensions.GetDatasetLengthAsync(dcmFile, recyclableMemoryStreamManager);

        // 128 bytes is the length of the header
        // 4 bytes is the length of the preamble
        // 212 bytes is the length of the file meta info
        // 44 bytes is each item value length + 8 bytes is the tag length
        // For patientName, 8 bytes is the tag length + 12 is the value length
        Assert.Equal(464, length);
    }

    [Fact]
    public async Task GivenNullDicomFile_WhenGetDatasetLengthAsyncIsCalled_ThrowsArgumentNullException()
    {
        DicomFile dcmFile = null;
        var recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        await Assert.ThrowsAsync<ArgumentNullException>(() => DicomFileExtensions.GetDatasetLengthAsync(dcmFile, recyclableMemoryStreamManager));
    }

    [Fact]
    public async Task GivenNullRecyclableMemoryStreamManager_WhenGetDatasetLengthAsyncIsCalled_ThrowsArgumentNullException()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() }
        };
        var dcmFile = new DicomFile(dataset);
        RecyclableMemoryStreamManager recyclableMemoryStreamManager = null;

        await Assert.ThrowsAsync<ArgumentNullException>(() => DicomFileExtensions.GetDatasetLengthAsync(dcmFile, recyclableMemoryStreamManager));
    }

    [Fact]
    public async Task Given_LargeDataset_WhenGetDatasetLengthAsyncIsCalled_ReturnsCorrectLength()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() }
        };
        dataset.AddOrUpdate(DicomTag.PixelData, new byte[100000]);
        var dcmFile = new DicomFile(dataset);

        var recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        var length = await DicomFileExtensions.GetDatasetLengthAsync(dcmFile, recyclableMemoryStreamManager);

        // 128 bytes is the length of the header
        // 4 bytes is the length of the preamble
        // 212 bytes is the length of the file meta info
        // 52 bytes is each item length
        // 100000 bytes is the length of the pixel data
        Assert.Equal(100456, length);
    }
}
