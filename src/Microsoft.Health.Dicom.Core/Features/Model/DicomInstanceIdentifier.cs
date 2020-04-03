// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features
{
    public class DicomInstanceIdentifier
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        public DicomInstanceIdentifier(
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            long? version = null)
        {
            EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid));
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid));

            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Version = version;
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public long? Version { get; }

        public override bool Equals(object obj)
        {
            if (obj is DicomInstanceIdentifier identity)
            {
                return StudyInstanceUid.Equals(identity.StudyInstanceUid, EqualsStringComparison) &&
                        SeriesInstanceUid.Equals(identity.SeriesInstanceUid, EqualsStringComparison) &&
                        SopInstanceUid.Equals(identity.SopInstanceUid, EqualsStringComparison);
            }

            return false;
        }

        public override int GetHashCode()
            => (StudyInstanceUid + SeriesInstanceUid + SopInstanceUid).GetHashCode(EqualsStringComparison);

        public override string ToString()
            => $"Study Instance Uid: {StudyInstanceUid}, Series Instance Uid: {SeriesInstanceUid}, SOP Instance Uid {SopInstanceUid}";
    }
}
