// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure;
using Azure.Data.Tables;
using EnsureThat;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities
{
    /// <summary>
    /// Entity used to represent a fhir intransient error
    /// </summary>
    public class IntransientEntity : IntransientError, ITableEntity
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
        : base(studyUid, seriesUid, instanceUid, changeFeedSequence, ex)
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
