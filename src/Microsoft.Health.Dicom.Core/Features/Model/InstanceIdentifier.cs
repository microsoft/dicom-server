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
            string partitionId = default)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            PartitionId = partitionId;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
        }

        public string PartitionId { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

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
            => (StudyInstanceUid + SeriesInstanceUid + SopInstanceUid).GetHashCode(EqualsStringComparison);

        public override string ToString()
            => $"StudyInstanceUID: {StudyInstanceUid}, SeriesInstanceUID: {SeriesInstanceUid}, SOPInstanceUID: {SopInstanceUid}";
    }
}
