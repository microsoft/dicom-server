// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to prepare request for and process response of a FHIR transaction.
    /// </summary>
    public interface IFhirTransactionPipelineStep
    {
        /// <summary>
        /// Asynchronously prepares the transaction request.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous preparation operation.</returns>
        Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes the transaction response.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        void ProcessResponse(FhirTransactionContext context);
    }
}
