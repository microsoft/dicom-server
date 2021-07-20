// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IObservationDeleteHandler
    {
        /// <summary>
        /// Create a transaction request entry to delete an existing DoseSummary observation based on the StudyInstanceUID provided
        /// in the transaction context.
        /// </summary>
        /// <remarks>
        /// - This currently only supports single observation deletion.
        /// - There _should_ only be a single observation per study instance -- but a users can technically create add
        ///   more as there is no built in 1:1 mapping in FHIR.
        /// - If multiple dose summaries are found mapping to the same study instance, we only delete the first one returned.
        /// </remarks>
        /// <param name="context">The transaction request context</param>
        /// <param name="cancellationToken">the cancellation token</param>
        /// <returns>a transaction request entry to delete a single Dose Summary if a matching one is found</returns>
        Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken);
    }
}
