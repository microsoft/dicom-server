// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Partition;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class VersionedInstanceIdentifier : InstanceIdentifier
{
    public VersionedInstanceIdentifier(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        long version,
        PartitionEntry partitionEntry)
        : base(studyInstanceUid, seriesInstanceUid, sopInstanceUid, partitionEntry)
    {
        Version = version;
    }

    public VersionedInstanceIdentifier(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        long version,
        int partitionKey = DefaultPartition.Key,
        string partitionName = DefaultPartition.Name)
        : base(studyInstanceUid, seriesInstanceUid, sopInstanceUid, partitionKey, partitionName)
    {
        Version = version;
    }

    public long Version { get; }

    public override bool Equals(object obj)
    {
        if (obj is VersionedInstanceIdentifier identifier)
        {
            return base.Equals(obj) && identifier.Version == Version;
        }

        return false;
    }

    public override int GetHashCode()
        => base.GetHashCode() ^ Version.GetHashCode();

    public override string ToString()
        => base.ToString() + $"Version: {Version}";
}
