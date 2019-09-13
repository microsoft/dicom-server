// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Persistence;

namespace Microsoft.Health.Dicom.Core.Features.Transaction
{
    public interface IDicomTransactionService
    {
        /// <summary>
        /// Attempts to start a transaction for the provided series.
        /// This call will wait until it has acquired a lock on the series, and will also clean-up previous transactions for this series
        /// if needed. Timeouts are recommended when calling this.
        /// </summary>
        /// <param name="dicomSeries">The series to sart a transaciton on a lock.</param>
        /// <param name="dicomInstances">The instances that will be modified during this transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created transaction.</returns>
        Task<ITransaction> BeginTransactionAsync(DicomSeries dicomSeries, DicomInstance[] dicomInstances, CancellationToken cancellationToken = default);

        Task<ITransaction> BeginTransactionAsync(DicomSeries dicomSeries, CancellationToken cancellationToken = default);
    }
}
