// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Model
{
    public class InstanceIdentifier
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        public InstanceIdentifier(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            string partitionId = null)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            PartitionId = partitionId;
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public string PartitionId { get; }

        public override bool Equals(object obj)
        {
            if (obj is InstanceIdentifier identifier)
            {
                return PartitionId.Equals(identifier.PartitionId, EqualsStringComparison) &&
                        StudyInstanceUid.Equals(identifier.StudyInstanceUid, EqualsStringComparison) &&
                        SeriesInstanceUid.Equals(identifier.SeriesInstanceUid, EqualsStringComparison) &&
                        SopInstanceUid.Equals(identifier.SopInstanceUid, EqualsStringComparison);
            }

            return false;
        }

        public override int GetHashCode()
            => (PartitionId + StudyInstanceUid + SeriesInstanceUid + SopInstanceUid).GetHashCode(EqualsStringComparison);

        public override string ToString()
            => $"PartitionId: {PartitionId}, StudyInstanceUID: {StudyInstanceUid}, SeriesInstanceUID: {SeriesInstanceUid}, SOPInstanceUID: {SopInstanceUid}";
    }
}
