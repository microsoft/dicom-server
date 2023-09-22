// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Model;
using CorePartition = Microsoft.Health.Dicom.Core.Features.Partitioning;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal class DicomInstanceId
{
    private const StringComparison EqualsStringComparison = StringComparison.Ordinal;
    public DicomInstanceId(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, Partition partition)
    {
        StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
        SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
        Partition = partition;
    }

    public string StudyInstanceUid { get; }
    public string SeriesInstanceUid { get; }
    public string SopInstanceUid { get; }
    public Partition Partition { get; }

    public static DicomInstanceId FromDicomFile(DicomFile dicomFile, Partition partition = null, string studyInstanceUid = null)
    {
        partition = partition ?? Partition.Default;
        InstanceIdentifier instanceIdentifier = dicomFile.Dataset.ToInstanceIdentifier(CorePartition.Partition.Default);
        return new DicomInstanceId(studyInstanceUid ?? instanceIdentifier.StudyInstanceUid, instanceIdentifier.SeriesInstanceUid, instanceIdentifier.SopInstanceUid, partition);
    }

    public override bool Equals(object obj)
    {
        if (obj is DicomInstanceId instanceId)
        {
            return StudyInstanceUid.Equals(instanceId.StudyInstanceUid, EqualsStringComparison) &&
                    SeriesInstanceUid.Equals(instanceId.SeriesInstanceUid, EqualsStringComparison) &&
                    SopInstanceUid.Equals(instanceId.SopInstanceUid, EqualsStringComparison) &&
                    Partition.Name.Equals(instanceId.Partition.Name, EqualsStringComparison);
        }

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Partition.Name);

}
