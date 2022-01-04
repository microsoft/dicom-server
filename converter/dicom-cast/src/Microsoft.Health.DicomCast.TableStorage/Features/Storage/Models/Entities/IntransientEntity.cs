// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure;
using Azure.Data.Tables;
using EnsureThat;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities
{
    /// <summary>
    /// Entity used to represent a fhir intransient error
    /// </summary>
    public class IntransientEntity : ITableEntity
    {
        public IntransientEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntransientEntity"/> class.
        /// </summary>
        /// <param name="studyUid">StudyUid of the changefeed entry that failed </param>
        /// <param name="seriesUid">SeriesUid of the changefeed entry that failed</param>
        /// <param name="instanceUid">InstanceUid of the changefeed entry that failed</param>
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

            StudyUid = studyUid;
            SeriesUid = seriesUid;
            InstanceUid = instanceUid;
            ChangeFeedSequence = changeFeedSequence;
            Exception = ex.ToString();
        }

        public string StudyUid { get; set; }

        public string SeriesUid { get; set; }

        public string InstanceUid { get; set; }

        public string Exception { get; set; }

        public long ChangeFeedSequence { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
