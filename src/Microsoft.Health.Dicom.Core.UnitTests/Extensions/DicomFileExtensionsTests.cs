// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.IO;
using Xunit;
using DicomFileExtensions = Microsoft.Health.Dicom.Core.Features.Retrieve.DicomFileExtensions;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions;
public class DicomFileExtensionsTests
{
    private const string TestDataRootFolder = "TranscodingSamples";

    [Fact]
    public async Task GivenNullDicomFile_WhenGetDatasetLengthAsyncIsCalled_ThrowsArgumentNullException()
    {
        DicomFile dcmFile = null;
        var recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        await Assert.ThrowsAsync<ArgumentNullException>(() => DicomFileExtensions.GetByteLengthAsync(dcmFile, recyclableMemoryStreamManager));
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

        await Assert.ThrowsAsync<ArgumentNullException>(() => DicomFileExtensions.GetByteLengthAsync(dcmFile, recyclableMemoryStreamManager));
    }

    [Theory]
    [MemberData(nameof(GetAllTestDatas))]
    public async Task GivenDicomFile_WhenGetDatasetLengthAsyncIsCalled_LengthShouldMatch(string fileName)
    {
        long expectedLength = 0;
        var inFile = DicomFile.Open(fileName);

        using (var stream = new MemoryStream())
        {
            await inFile.SaveAsync(stream);
            expectedLength = stream.Length;
        }

        var length = await DicomFileExtensions.GetByteLengthAsync(inFile, new RecyclableMemoryStreamManager());
        Assert.Equal(expectedLength, length);
    }

    public static IEnumerable<object[]> GetAllTestDatas()
    {
        foreach (string path in Directory.EnumerateFiles(TestDataRootFolder, "*", SearchOption.AllDirectories))
        {
            yield return new object[] { path };
        }
    }
}
