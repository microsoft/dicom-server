// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Provides functionality to execute a transaction in a FHIR server.
    /// </summary>
    public interface IFhirTransactionExecutor
    {
        /// <summary>
        /// Asynchronously executes a FHIR transaction.
        /// </summary>
        /// <param name="bundle">The transaction to execute..</param>
        /// <param name="cancellationToken">The cancellation token/</param>
        /// <returns>A task representing the processing operation.</returns>
        Task<Bundle> ExecuteTransactionAsync(Bundle bundle, CancellationToken cancellationToken);
    }
}
