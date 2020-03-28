// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features
{
    public class DicomDatasetIdentifier
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        private DicomDatasetIdentifier(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            EnsureArg.IsNotNull(studyInstanceUid);
            EnsureArg.IsNotNull(seriesInstanceUid);
            EnsureArg.IsNotNull(sopInstanceUid);

            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
        }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public static DicomDatasetIdentifier Create(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new DicomDatasetIdentifier(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty));
        }

        public override bool Equals(object obj)
        {
            if (obj is DicomDatasetIdentifier identity)
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
