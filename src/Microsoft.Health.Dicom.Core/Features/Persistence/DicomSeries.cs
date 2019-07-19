// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomSeries : IDicomResource
    {
        [JsonConstructor]
        public DicomSeries(string studyInstanceUID, string seriesInstanceUID)
        {
            // Run the instance identifiers through the regular expression check.
            EnsureArg.IsTrue(DicomIdentifierValidator.IdentifierRegex.IsMatch(studyInstanceUID), nameof(studyInstanceUID));
            EnsureArg.IsTrue(DicomIdentifierValidator.IdentifierRegex.IsMatch(seriesInstanceUID), nameof(seriesInstanceUID));
            EnsureArg.IsNotEqualTo(studyInstanceUID, seriesInstanceUID, nameof(seriesInstanceUID));

            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
        }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public override bool Equals(object obj)
        {
            if (obj is DicomSeries identity)
            {
                return StudyInstanceUID.Equals(identity.StudyInstanceUID, DicomStudy.EqualsStringComparison) &&
                        SeriesInstanceUID.Equals(identity.SeriesInstanceUID, DicomStudy.EqualsStringComparison);
            }

            return false;
        }

        public override int GetHashCode()
            => (StudyInstanceUID + SeriesInstanceUID).GetHashCode(DicomStudy.EqualsStringComparison);
    }
}
