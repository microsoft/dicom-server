// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to process a FHIR transaction.
    /// </summary>
    public interface IFhirTransactionPipeline
    {
        /// <summary>
        /// Asynchronously processes a FHIR transaction from <see cref="ChangeFeedEntry"/>.
        /// </summary>
        /// <param name="changeFeedEntry">The change feed entry to process.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous processing operation.</returns>
        Task ProcessAsync(ChangeFeedEntry changeFeedEntry, CancellationToken cancellationToken);
    }
}
