// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;

public class DicomPrefixedFileNameBuilder : IDicomFileNameBuilder
{
    public virtual string GetInstanceFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return $"{GetPrefix(instanceIdentifier.Version)}_{instanceIdentifier.Version}.dcm";
    }

    public virtual string GetMetadataFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return $"{GetPrefix(instanceIdentifier.Version)}_{instanceIdentifier.Version}_metadata.json";
    }

    public virtual string GetWorkItemFileName(long version)
    {
        EnsureArg.IsGt(version, 0, nameof(version));
        return $"{GetPrefix(version)}_{version}_workitem.json";
    }

    private static string GetPrefix(long version)
    {
        int lowerThree = HashingHelper.GetXxHashCode(version) & 0x00000FFF;
        return lowerThree.ToString("X3"); // Do not need substring as we masked out the other values
    }
}
