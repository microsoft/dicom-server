// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using EnsureThat;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents each change feed entry of a change has retrieved from the store
    /// </summary>
    public class ChangeFeedEntry
    {
        public ChangeFeedEntry(
            long sequence,
            DateTime timeStamp,
            ChangeFeedAction action,
            string studyInstanceUid,
            string seriesInstanceUid,
            string sopInstanceUid,
            ChangeFeedState state)
        {
            EnsureArg.IsNotNull(studyInstanceUid);
            EnsureArg.IsNotNull(seriesInstanceUid);
            EnsureArg.IsNotNull(sopInstanceUid);

            Sequence = sequence;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Action = action;
            TimeStamp = timeStamp;
            State = state;
        }

        public long Sequence { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeFeedAction Action { get; }

        public DateTime TimeStamp { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeFeedState State { get; }

        public DicomDataset Metadata { get; }
    }
}
