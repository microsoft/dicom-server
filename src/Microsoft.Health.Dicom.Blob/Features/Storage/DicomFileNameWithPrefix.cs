// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Blob.Utilities;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Blob.Features.Storage;
public class DicomFileNameWithPrefix : IDicomFileNameBuilder
{
    public const int MaxPrefixLength = 3;

    public string GetInstanceFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        return $"{HashingHelper.ComputeXXHash(instanceIdentifier.Version, MaxPrefixLength)}_{instanceIdentifier.Version}.dcm";
    }

    public string GetMetadataFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return $"{HashingHelper.ComputeXXHash(instanceIdentifier.Version, MaxPrefixLength)}_{instanceIdentifier.Version}_metadata.json";
    }
}
