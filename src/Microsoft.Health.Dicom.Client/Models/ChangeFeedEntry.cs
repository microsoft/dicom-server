// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json.Serialization;
using EnsureThat;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents each change feed entry of a change has retrieved from the store
    /// </summary>
    public class ChangeFeedEntry
    {
        public ChangeFeedEntry(
            long sequence,
            DateTime timestamp,
            ChangeFeedAction action,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            ChangeFeedState state,
            string partitionName = default)
        {
            EnsureArg.IsNotNull(studyInstanceUid);
            EnsureArg.IsNotNull(seriesInstanceUid);
            EnsureArg.IsNotNull(sopInstanceUid);

            Sequence = sequence;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Action = action;
            Timestamp = timestamp;
            State = state;
            PartitionName = partitionName;
        }

        public long Sequence { get; }

        public string PartitionName { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ChangeFeedAction Action { get; }

        public DateTimeOffset Timestamp { get; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ChangeFeedState State { get; }

        public DicomDataset Metadata { get; set; }
    }
}
