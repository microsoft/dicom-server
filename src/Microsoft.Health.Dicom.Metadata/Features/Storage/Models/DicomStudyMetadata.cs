// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    internal class DicomStudyMetadata
    {
        public DicomStudyMetadata(string studyInstanceUID)
        {
            EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUID, nameof(studyInstanceUID));

            StudyInstanceUID = studyInstanceUID;
            SeriesMetadata = new Dictionary<string, DicomSeriesMetadata>();
        }

        [JsonConstructor]
        public DicomStudyMetadata(string studyInstanceUID, IDictionary<string, DicomSeriesMetadata> seriesMetadata)
            : this(studyInstanceUID)
        {
            EnsureArg.IsNotNull(seriesMetadata, nameof(seriesMetadata));

            SeriesMetadata = seriesMetadata;
        }

        [JsonProperty("studyId")]
        public string StudyInstanceUID { get; }

        [JsonProperty("seriesMetadata")]
        public IDictionary<string, DicomSeriesMetadata> SeriesMetadata { get; }
    }
}
