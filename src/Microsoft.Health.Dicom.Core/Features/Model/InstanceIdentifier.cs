// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class InstanceIdentifier
{
    private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

    public InstanceIdentifier(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        Partition partition)
    {
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
        StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
        SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
    }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    public Partition Partition { get; }

    public override bool Equals(object obj)
    {
        if (obj is InstanceIdentifier identifier)
        {
            return StudyInstanceUid.Equals(identifier.StudyInstanceUid, EqualsStringComparison) &&
                    SeriesInstanceUid.Equals(identifier.SeriesInstanceUid, EqualsStringComparison) &&
                    SopInstanceUid.Equals(identifier.SopInstanceUid, EqualsStringComparison);
        }
        return false;
    }

    public override int GetHashCode()
        => (Partition.Key + StudyInstanceUid + SeriesInstanceUid + SopInstanceUid).GetHashCode(EqualsStringComparison);

    public override string ToString()
        => $"PartitionKey: {Partition.Key}, StudyInstanceUID: {StudyInstanceUid}, SeriesInstanceUID: {SeriesInstanceUid}, SOPInstanceUID: {SopInstanceUid}";
}
