// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Blob.Features.Storage;
using Microsoft.Health.Dicom.Blob.Utilities;
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
        var version = 1;
        Assert.Equal($"{HashingHelper.ComputeXXHash(version, 3)}_{version}.dcm", _nameWithPrefix.GetInstanceFileName(version));
        Assert.Equal($"{HashingHelper.ComputeXXHash(version, 3)}_{version}_metadata.json", _nameWithPrefix.GetMetadataFileName(version));
        Assert.Equal($"{HashingHelper.ComputeXXHash(version, 3)}_{version}_frames_range.json", _nameWithPrefix.GetInstanceFramesRangeFileName(version));
        Assert.Equal($"{HashingHelper.ComputeXXHash(version, 3)}_ {version}_frames_range.json", _nameWithPrefix.GetInstanceFramesRangeFileNameWithSpace(version));
    }
}
