// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models.Entities
{
    public class RetryableEntity : TableEntity
    {
        public RetryableEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryableEntity"/> class.
        /// </summary>
        /// <param name="studyUid">StudyUID of the changefeed entry that failed </param>
        /// <param name="seriesUid">SeriesUID of the changefeed entry that failed</param>
        /// <param name="instanceUid">InstanceUID of the changefeed entry that failed</param>
        /// <param name="changeFeedSequence">Changefeed sequence number that threw exception</param>
        /// <param name="retryNum">Number of times changefeed entry has been retried</param>
        /// <param name="ex">The exception that was thrown</param>
        public RetryableEntity(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, int retryNum, Exception ex)
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
            RetryNumber = retryNum;
            Exception = ex.ToString();
        }

        public string StudyUID { get; set; }

        public string SeriesUID { get; set; }

        public string InstanceUID { get; set; }

        public long ChangeFeedSequence { get; set; }

        public int RetryNumber { get; set; }

        public string Exception { get; set; }
    }
}
