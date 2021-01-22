// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities
{
    /// <summary>
    /// Entity used to represent a fhir intransient error
    /// </summary>
    public class IntransientEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntransientEntity"/> class.
        /// </summary>
        /// <param name="studyUid">StudyUID of the changefeed entry that failed </param>
        /// <param name="seriesUid">SeriesUID of the changefeed entry that failed</param>
        /// <param name="instanceUid">InstanceUID of the changefeed entry that failed</param>
        /// <param name="changeFeedSequence">Changefeed sequence number that threw exception</param>
        /// <param name="ex">The exception that was thrown</param>
        public IntransientEntity(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception ex)
        {
            EnsureArg.IsNotNull(studyUid, nameof(studyUid));
            EnsureArg.IsNotNull(seriesUid, nameof(seriesUid));
            EnsureArg.IsNotNull(instanceUid, nameof(instanceUid));
            EnsureArg.IsNotNull(ex, nameof(ex));

            PartitionKey = ex.GetType().Name;
            RowKey = Guid.NewGuid().ToString();

            StudyUID = studyUid;
            SeriesUID = seriesUid;
            InstanceUID = instanceUid;
            ChangeFeedSequence = changeFeedSequence;
            Exception = ex.ToString();
        }

        public string StudyUID { get; set; }

        public string SeriesUID { get; set; }

        public string InstanceUID { get; set; }

        public string Exception { get; set; }

        public long ChangeFeedSequence { get; set; }
    }
}
