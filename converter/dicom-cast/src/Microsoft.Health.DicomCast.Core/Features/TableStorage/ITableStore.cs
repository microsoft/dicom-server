// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;

namespace Microsoft.Health.DicomCast.Core.Features.TableStorage
{
    /// <summary>
    /// Store persistent data in table store
    /// </summary>
    public interface ITableStore
    {
        /// <summary>
        /// Store an exception to an azure storage table.
        /// </summary>
        /// <param name="changeFeedEntry">ChangeFeedEntry that threw exception</param>
        /// <param name="exceptionToStore">The exception that was thrown and needs to be stored</param>
        /// <param name="errorType">The type of error thrown</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StoreExceptionToTable(ChangeFeedEntry changeFeedEntry, Exception exceptionToStore, ErrorType errorType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Store a retryable exception to an azure storage table.
        /// </summary>
        /// <param name="changeFeedEntry">ChangeFeedEntry that threw exception</param>
        /// <param name="retryNum">Number of times this changefeed entry has been tried</param>
        /// <param name="exceptionToStore">The exception that was thrown and needs to be stored</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StoreRetryableExceptionToTable(ChangeFeedEntry changeFeedEntry, int retryNum, Exception exceptionToStore, CancellationToken cancellationToken);
    }
}
