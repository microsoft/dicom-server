// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ChangeFeed
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

        public ChangeFeedAction Action { get; }

        public DateTime TimeStamp { get; }

        public ChangeFeedState State { get; }
    }
}
