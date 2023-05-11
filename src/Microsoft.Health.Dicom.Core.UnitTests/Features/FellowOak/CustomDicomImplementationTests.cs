// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.FellowOak;

public class CustomDicomImplementationTests
{
    private readonly DicomUID _expectedClassUID;
    private readonly string _expectedVersion;

    public CustomDicomImplementationTests()
    {
        (DicomUID classUID, string versionName) = Samples.GetDicomImplemenationClasUIDAndVersionName();
        _expectedClassUID = classUID;
        _expectedVersion = versionName;
        CustomDicomImplementation.SetDicomImplementationClassUIDAndVersion();
    }

    [Fact]
    public void SetFellowOakDicomImplementation_SetsClassUID()
    {
        Assert.Equal(_expectedClassUID, DicomImplementation.ClassUID);
    }

    [Fact]
    public void SetFellowOakDicomImplementation_SetsVersion()
    {
        Assert.Equal(_expectedVersion, DicomImplementation.Version);
    }

    [Fact]
    public async Task GivenDataset_WhenDicomFileIsSaved_DicomImplementationIsSetCorrectly()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, TestUidGenerator.Generate() },
            { DicomTag.SOPInstanceUID, TestUidGenerator.Generate() }
        };

        CustomDicomImplementation.SetDicomImplementationClassUIDAndVersion();
        var dcmFile = new DicomFile(dataset);

        using var stream = new MemoryStream();
        await dcmFile.SaveAsync(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var actualDcmFile = await DicomFile.OpenAsync(stream);

        Assert.Equal(_expectedClassUID, actualDcmFile.FileMetaInfo.ImplementationClassUID);
        Assert.Equal(_expectedVersion, actualDcmFile.FileMetaInfo.ImplementationVersionName);
    }
}
