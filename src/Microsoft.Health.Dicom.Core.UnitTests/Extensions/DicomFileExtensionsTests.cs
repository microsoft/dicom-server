// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    private static DirectoryInfo TestDataDirectory => new DirectoryInfo(Path.Combine(".", "TestFiles"));

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

    [Theory]
    [InlineData("input1.dcm")]
    [InlineData("input2.dcm")]
    [InlineData("input3.dcm")]
    public async Task GivenDicomFile_WhenGetDatasetLengthAsyncIsCalled_LengthShouldMatch(string fileName)
    {
        long expectedLength = 0;

        var inFile = DicomFile.Open(Resolve(fileName));
        using (var file = inFile.File.OpenRead())
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                expectedLength = stream.Length;
            }
        }

        var length = await DicomFileExtensions.GetDatasetLengthAsync(inFile, new RecyclableMemoryStreamManager());
        Assert.Equal(expectedLength, length);
    }

    private static string Resolve(string path) => Path.Combine(TestDataDirectory.FullName, path);
}
