// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.Worker
{
    /// <summary>
    /// Provides functionality to process the change feed.
    /// </summary>
    public interface IChangeFeedProcessor
    {
        /// <summary>
        /// Asynchronously processes the change feed.
        /// </summary>
        /// <param name="pollIntervalDuringCatchup">The delay between polls during catchup phase.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        Task ProcessAsync(TimeSpan pollIntervalDuringCatchup, CancellationToken cancellationToken);
    }
}
