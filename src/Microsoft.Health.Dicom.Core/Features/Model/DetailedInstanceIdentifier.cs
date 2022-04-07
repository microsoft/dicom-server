// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class DetailedInstanceIdentifier : VersionedInstanceIdentifier
{
    public DetailedInstanceIdentifier(
        string studyInstanceUid,
        long studyKey,
        string seriesInstanceUid,
        long seriesKey,
        string sopInstanceUid,
        long sopKey,
        long version,
        MigrationState migrationState = MigrationState.NotStarted,
        int partitionKey = default)
        : base(studyInstanceUid, seriesInstanceUid, sopInstanceUid, version, partitionKey)
    {
        StudyKey = studyKey;
        SeriesKey = seriesKey;
        SopKey = sopKey;
        MigrationState = migrationState;
    }

    public long StudyKey { get; }

    public long SeriesKey { get; }
    public long SopKey { get; }

    public MigrationState MigrationState { get; }

    public override bool Equals(object obj)
    {
        if (obj is DetailedInstanceIdentifier identifier)
        {
            return base.Equals(obj) &&
                identifier.StudyKey == StudyKey &&
                identifier.SeriesKey == SeriesKey &&
                identifier.SopKey == SopKey &&
                identifier.MigrationState == MigrationState;
        }

        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), StudyKey, SeriesKey, SopKey, MigrationState);

    public override string ToString()
        => base.ToString() + $", StudyKey: {StudyKey}, SeriesKey: {SeriesKey}, SopKey: {SopKey}, MigrationState: {MigrationState}";
}
