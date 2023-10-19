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

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

// TODO: Replace with InstanceId after reconciling models between client and server
internal class DicomInstanceId : IEquatable<DicomInstanceId>
{
    public DicomInstanceId(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, Partition partition)
    {
        StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        SeriesInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
        SopInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));
        Partition = EnsureArg.IsNotNull(partition, nameof(partition));
    }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    public Partition Partition { get; }

    public static DicomInstanceId FromDicomFile(DicomFile dicomFile, Partition partition = null, string studyInstanceUid = null)
    {
        InstanceIdentifier instanceIdentifier = dicomFile.Dataset.ToInstanceIdentifier(Core.Features.Partitioning.Partition.Default);
        return new DicomInstanceId(
            studyInstanceUid ?? instanceIdentifier.StudyInstanceUid,
            instanceIdentifier.SeriesInstanceUid,
            instanceIdentifier.SopInstanceUid,
            partition ?? Partition.Default);
    }

    public override bool Equals(object obj)
        => obj is DicomInstanceId other && Equals(other);

    public bool Equals(DicomInstanceId other)
    {
        return other is not null
            && StudyInstanceUid.Equals(other.StudyInstanceUid, StringComparison.Ordinal)
            && SeriesInstanceUid.Equals(other.SeriesInstanceUid, StringComparison.Ordinal)
            && SopInstanceUid.Equals(other.SopInstanceUid, StringComparison.Ordinal)
            && Partition.Name.Equals(other.Partition.Name, StringComparison.Ordinal);
    }

    public override int GetHashCode()
        => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, Partition.Name);

    public override string ToString()
        => $"PartitionKey: {Partition.Key}, StudyInstanceUID: {StudyInstanceUid}, SeriesInstanceUID: {SeriesInstanceUid}, SOPInstanceUID: {SopInstanceUid}";
}
