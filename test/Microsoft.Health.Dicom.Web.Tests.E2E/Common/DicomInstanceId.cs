// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using PartitionEntry = Microsoft.Health.Dicom.Client.Models.PartitionEntry;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal class DicomInstanceId
{
    private const StringComparison EqualsStringComparison = StringComparison.Ordinal;
    public DicomInstanceId(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, PartitionEntry partitionEntry)
    {
        StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
        SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
        PartitionEntry = partitionEntry;
    }

    public string StudyInstanceUid { get; }
    public string SeriesInstanceUid { get; }
    public string SopInstanceUid { get; }
    public PartitionEntry PartitionEntry { get; }

    public static DicomInstanceId FromDicomFile(DicomFile dicomFile, PartitionEntry partitionEntry = null)
    {
        partitionEntry = partitionEntry ?? PartitionEntry.Default;
        InstanceIdentifier instanceIdentifier = dicomFile.Dataset.ToInstanceIdentifier(DefaultPartition.PartitionEntry);
        return new DicomInstanceId(instanceIdentifier.StudyInstanceUid, instanceIdentifier.SeriesInstanceUid, instanceIdentifier.SopInstanceUid, partitionEntry);
    }

    public override bool Equals(object obj)
    {
        if (obj is DicomInstanceId instanceId)
        {
            return StudyInstanceUid.Equals(instanceId.StudyInstanceUid, EqualsStringComparison) &&
                    SeriesInstanceUid.Equals(instanceId.SeriesInstanceUid, EqualsStringComparison) &&
                    SopInstanceUid.Equals(instanceId.SopInstanceUid, EqualsStringComparison) &&
                    PartitionEntry.PartitionName.Equals(instanceId.PartitionEntry.PartitionName, EqualsStringComparison);
        }

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, PartitionEntry.PartitionName);

}
