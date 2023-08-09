// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class InstanceMetadata
{
    public InstanceMetadata(VersionedInstanceIdentifier versionedInstanceIdentifier, InstanceProperties instanceProperties)
    {
        VersionedInstanceIdentifier = EnsureArg.IsNotNull(versionedInstanceIdentifier, nameof(versionedInstanceIdentifier));
        InstanceProperties = EnsureArg.IsNotNull(instanceProperties, nameof(instanceProperties));
    }

    public VersionedInstanceIdentifier VersionedInstanceIdentifier { get; }

    public InstanceProperties InstanceProperties { get; }

    public long GetVersion(bool isOriginalVersionRequested)
    {
        if (isOriginalVersionRequested && InstanceProperties.OriginalVersion.HasValue)
        {
            return InstanceProperties.OriginalVersion.Value;
        }

        return VersionedInstanceIdentifier.Version;
    }

    public InstanceFileState ToInstanceFileState()
    {
        return new InstanceFileState
        {
            Version = VersionedInstanceIdentifier.Version,
            OriginalVersion = InstanceProperties.OriginalVersion,
            NewVersion = InstanceProperties.NewVersion
        };
    }
}
