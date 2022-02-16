// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Models.Entities
{
    public class RetryableEntity : RetryableError, ITableEntity
    {
        public RetryableEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryableEntity"/> class.
        /// </summary>
        /// <param name="studyUid">StudyUid of the changefeed entry that failed </param>
        /// <param name="seriesUid">SeriesUid of the changefeed entry that failed</param>
        /// <param name="instanceUid">InstanceUid of the changefeed entry that failed</param>
        /// <param name="changeFeedSequence">Changefeed sequence number that threw exception</param>
        /// <param name="retryNum">Number of times changefeed entry has been retried</param>
        /// <param name="ex">The exception that was thrown</param>
        public RetryableEntity(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, int retryNum, Exception ex)
            : base(studyUid, seriesUid, instanceUid, changeFeedSequence, retryNum, ex)
        {
            EnsureArg.IsNotNull(ex, nameof(ex));

            PartitionKey = ex.GetType().Name;
            RowKey = Guid.NewGuid().ToString();
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
