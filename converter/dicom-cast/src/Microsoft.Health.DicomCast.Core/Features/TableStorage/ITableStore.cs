// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.ExceptionStorage
{
    /// <summary>
    /// Persistent data in table store
    /// </summary>
    public interface ITableStore
    {
        /// <summary>
        /// Store an exception to an azure storage table.
        /// </summary>
        /// <param name="studyUid">StudyUID of dicom instance that threw exception</param>
        /// <param name="seriesUid">SeriesUID of dicom instance that threw exception</param>
        /// <param name="instanceUid">InstanceUID of dicom instance that threw exception</param>
        /// <param name="changeFeedSequence">Changefeed sequence number that threw exception</param>
        /// <param name="exceptionToStore">The exception that was thrown and needs to be stored</param>
        /// <param name="errorType">The type of error thrown</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StoreExceptionToTable(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default);
    }
}
