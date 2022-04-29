// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Common;

public class DicomFileNameWithUIDTests
{
    private readonly DicomFileNameWithUID _nameWithUID;

    public DicomFileNameWithUIDTests()
    {
        _nameWithUID = new DicomFileNameWithUID();
    }

    [Fact]
    public void GivenIdentifier_GetFileNames_ShouldReturnExpectedValues()
    {
        VersionedInstanceIdentifier instanceIdentifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        Assert.Equal($"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}.dcm", _nameWithUID.GetInstanceFileName(instanceIdentifier));
        Assert.Equal($"{instanceIdentifier.StudyInstanceUid}/{instanceIdentifier.SeriesInstanceUid}/{instanceIdentifier.SopInstanceUid}_{instanceIdentifier.Version}_metadata.json", _nameWithUID.GetMetadataFileName(instanceIdentifier));
    }
}
