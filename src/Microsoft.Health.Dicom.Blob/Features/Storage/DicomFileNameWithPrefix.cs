// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Blob.Utilities;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

public class DicomFileNameWithPrefix : IDicomFileNameBuilder
{
    public const int MaxPrefixLength = 3;

    public string GetInstanceFileName(long version)
    {
        return $"{HashingHelper.ComputeXXHash(version, MaxPrefixLength)}_{version}.dcm";
    }

    public string GetMetadataFileName(long version)
    {
        return $"{HashingHelper.ComputeXXHash(version, MaxPrefixLength)}_{version}_metadata.json";
    }

    public string GetInstanceFramesRangeFileName(long version)
    {
        return $"{HashingHelper.ComputeXXHash(version, MaxPrefixLength)}_{version}_frames_range.json";
    }

    /// <summary>
    /// This method is used for the fallback logic to get the blob file with space in between
    /// that was introduced in a recent regression.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public string GetInstanceFramesRangeFileNameWithSpace(long version)
    {
        return $"{HashingHelper.ComputeXXHash(version, MaxPrefixLength)}_ {version}_frames_range.json";
    }
}
