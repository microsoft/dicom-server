// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Core.Features.Event
{
    public class DicomInstanceCreatedEventData
    {
        [JsonProperty(PropertyName = "studyInstanceUid")]
        public string StudyInstanceUid { get; set; }

        [JsonProperty(PropertyName = "seriesInstanceUid")]
        public string SeriesInstanceUid { get; set; }

        [JsonProperty(PropertyName = "sopInstanceUid")]
        public string SopInstanceUid { get; set; }
    }
}
