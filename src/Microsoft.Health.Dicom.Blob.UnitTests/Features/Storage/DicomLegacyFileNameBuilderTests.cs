// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Storage;

public class DicomLegacyFileNameBuilderTests
{
    private readonly DicomLegacyFileNameBuilder _nameWithUid = new DicomLegacyFileNameBuilder();

    [Fact]
    public void GivenIdentifier_GetFileNames_ShouldReturnExpectedValues()
    {
        var instanceIdentifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        Assert.Equal($"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}.dcm", _nameWithUid.GetInstanceFileName(instanceIdentifier));
        Assert.Equal($"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}_metadata.json", _nameWithUid.GetMetadataFileName(instanceIdentifier));
    }
}
