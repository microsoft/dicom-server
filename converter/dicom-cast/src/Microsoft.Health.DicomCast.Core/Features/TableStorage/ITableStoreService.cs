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
    /// Service that supports storing to azure table storage.
    /// </summary>
    public interface ITableStoreService
    {
        /// <summary>
        /// Store an exception to an azure storage table.
        /// </summary>
        /// <param name="studyUid">StudyUID of dicom instance that threw exception</param>
        /// <param name="seriesUid">SeriesUID of dicom instance that threw exception</param>
        /// <param name="instanceUid">InstanceUID of dicom instance that threw exception</param>
        /// <param name="exceptionToStore">The exception that was thrown and needs to be stored</param>
        /// <param name="errorType">The type of error thrown</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StoreException(string studyUid, string seriesUid, string instanceUid, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken = default);
    }
}
