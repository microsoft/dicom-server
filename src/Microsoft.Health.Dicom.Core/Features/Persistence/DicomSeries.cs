// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomSeries
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        [JsonConstructor]
        public DicomSeries(string studyInstanceUID, string seriesInstanceUID)
        {
            // Run the instance identifiers through the regular expression check.
            EnsureArg.Matches(studyInstanceUID, DicomIdentifierValidator.IdentifierRegex, nameof(studyInstanceUID));
            EnsureArg.Matches(seriesInstanceUID, DicomIdentifierValidator.IdentifierRegex, nameof(seriesInstanceUID));
            EnsureArg.IsNotEqualTo(studyInstanceUID, seriesInstanceUID, nameof(seriesInstanceUID));

            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
        }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public static DicomSeries Create(DicomDataset dicomDataset)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
            return new DicomSeries(
                dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));
        }

        public override bool Equals(object obj)
        {
            if (obj is DicomSeries identity)
            {
                return StudyInstanceUID.Equals(identity.StudyInstanceUID, EqualsStringComparison) &&
                        SeriesInstanceUID.Equals(identity.SeriesInstanceUID, EqualsStringComparison);
            }

            return false;
        }

        public override int GetHashCode()
            => (StudyInstanceUID + SeriesInstanceUID).GetHashCode(EqualsStringComparison);
    }
}
