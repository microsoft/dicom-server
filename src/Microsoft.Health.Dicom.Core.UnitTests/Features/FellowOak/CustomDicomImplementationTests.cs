// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.FellowOak;

public class CustomDicomImplementationTests
{
    [Fact]
    public void SetFellowOakDicomImplementation_SetsClassUID()
    {
        CustomDicomImplementation.SetFellowOakDicomImplementation();

        Assert.Equal(new DicomUID("1.3.6.1.4.1.311.129", "Implementation Class UID", DicomUidType.Unknown), DicomImplementation.ClassUID);
    }

    [Fact]
    public void SetFellowOakDicomImplementation_SetsVersion()
    {
        Version version = typeof(CustomDicomImplementation).GetTypeInfo().Assembly.GetName().Version;
        string expectedVersion = $"{version.Major}.{version.Minor}.{version.Build}";

        CustomDicomImplementation.SetFellowOakDicomImplementation();

        Assert.Equal(expectedVersion, DicomImplementation.Version);
    }

    [Fact]
    public async Task GivenDataset_WhenDicomFileIsSaved_DicomImplementationIsSetCorrectly()
    {
        Version version = typeof(CustomDicomImplementation).GetTypeInfo().Assembly.GetName().Version;
        string expectedVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        var expectedUID = new DicomUID("1.3.6.1.4.1.311.129", "Implementation Class UID", DicomUidType.Unknown);

        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() }
        };

        CustomDicomImplementation.SetFellowOakDicomImplementation();
        var dcmFile = new DicomFile(dataset);

        using var stream = new MemoryStream();
        await dcmFile.SaveAsync(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var actualDcmFile = await DicomFile.OpenAsync(stream);

        Assert.Equal(expectedUID, actualDcmFile.FileMetaInfo.ImplementationClassUID);
        Assert.Equal(expectedVersion, actualDcmFile.FileMetaInfo.ImplementationVersionName);
    }
}
