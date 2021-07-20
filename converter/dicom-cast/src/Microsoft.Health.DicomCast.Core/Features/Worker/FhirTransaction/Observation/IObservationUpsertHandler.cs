// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public interface IObservationUpsertHandler
    {
        /// <summary>
        /// Creates a transaction request to either update or create a Dose Summary observation based on the information
        /// found in the DicomDataset provided in the context.
        /// </summary>
        /// <param name="context">The transaction context</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A transaction request entry containing either a PUT or POST request to create a Dose Summary observation</returns>
        Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken);
    }
}
