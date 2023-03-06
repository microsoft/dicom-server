// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class VersionedInstanceIdentifier : InstanceIdentifier
{
    public VersionedInstanceIdentifier(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        long version,
        int partitionKey = default,
        int revision = 1)
        : base(studyInstanceUid, seriesInstanceUid, sopInstanceUid, partitionKey)
    {
        Version = version;
        Revision = revision;
    }

    public long Version { get; }

    public int Revision { get; }

    public override bool Equals(object obj)
    {
        if (obj is VersionedInstanceIdentifier identifier)
        {
            return base.Equals(obj) && identifier.Version == Version && identifier.Revision == Revision;
        }

        return false;
    }

    public override int GetHashCode()
        => base.GetHashCode() ^ Version.GetHashCode() ^ Revision.GetHashCode();

    public override string ToString()
        => base.ToString() + $"Version: {Version} Revision: {Revision}";
}
