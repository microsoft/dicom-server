// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.Worker
{
    /// <summary>
    /// The worker for DicomCast.
    /// </summary>
    public interface IDicomCastWorker
    {
        /// <summary>
        /// Asynchronously executes the worker.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents asynchronous worker execution.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
