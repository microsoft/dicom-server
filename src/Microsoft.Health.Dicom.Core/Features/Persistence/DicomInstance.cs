// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomInstance : IEquatable<DicomInstance>
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        public DicomInstance(string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
        {
            EnsureArg.IsNotNull(studyInstanceUID, nameof(studyInstanceUID));
            EnsureArg.IsNotNull(seriesInstanceUID, nameof(seriesInstanceUID));
            EnsureArg.IsNotNull(sopInstanceUID, nameof(sopInstanceUID));

            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            SopInstanceUID = sopInstanceUID;
        }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string SopInstanceUID { get; }

        public bool Equals(DicomInstance instance)
        {
            if (instance == null)
            {
                return false;
            }

            return StudyInstanceUID.Equals(instance.StudyInstanceUID, EqualsStringComparison) &&
                        SeriesInstanceUID.Equals(instance.SeriesInstanceUID, EqualsStringComparison) &&
                        SopInstanceUID.Equals(instance.SopInstanceUID, EqualsStringComparison);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DicomInstance);
        }

        public override int GetHashCode()
            => (StudyInstanceUID + SeriesInstanceUID + SopInstanceUID).GetHashCode(EqualsStringComparison);

        public override string ToString()
            => $"Study Instance UID: {StudyInstanceUID}, Series Instance UID: {SeriesInstanceUID}, SOP Instance UID {SopInstanceUID}";

        public static DicomInstance Create(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new DicomInstance(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty));
        }
    }
}
