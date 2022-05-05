// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Common;

public class DicomFileNameWithPrefixTests
{
    private readonly DicomFileNameWithPrefix _nameWithPrefix;

    public DicomFileNameWithPrefixTests()
    {
        _nameWithPrefix = new DicomFileNameWithPrefix();
    }

    [Fact]
    public void GivenIdentifier_GetFileNames_ShouldReturnExpectedValues()
    {
        VersionedInstanceIdentifier instanceIdentifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        Assert.Equal($"{HashingHelper.ComputeXXHash(instanceIdentifier.Version, 3)}_{instanceIdentifier.Version}.dcm", _nameWithPrefix.GetInstanceFileName(instanceIdentifier));
        Assert.Equal($"{HashingHelper.ComputeXXHash(instanceIdentifier.Version, 3)}_{instanceIdentifier.Version}_metadata.json", _nameWithPrefix.GetMetadataFileName(instanceIdentifier));
    }
}
