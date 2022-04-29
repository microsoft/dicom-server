// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Common;
public class DicomFileNameWithPrefix : IDicomFileNameBuilder
{
    public static readonly int MAXPREFIXLENGTH = 3;

    public string GetInstanceFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));

        return $"{HashingHelper.Hash(instanceIdentifier.Version, MAXPREFIXLENGTH)}_{instanceIdentifier.Version}.dcm";
    }

    public string GetMetadataFileName(VersionedInstanceIdentifier instanceIdentifier)
    {
        EnsureArg.IsNotNull(instanceIdentifier, nameof(instanceIdentifier));
        return $"{HashingHelper.Hash(instanceIdentifier.Version, MAXPREFIXLENGTH)}_{instanceIdentifier.Version}_metadata.json";
    }
}
