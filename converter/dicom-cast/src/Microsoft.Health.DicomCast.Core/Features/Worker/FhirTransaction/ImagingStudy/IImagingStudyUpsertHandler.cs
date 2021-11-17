// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Builds the request for creating or updating the <see cref="ImagingStudy"/> resource.
    /// </summary>
    public interface IImagingStudyUpsertHandler
    {
        /// <summary>
        /// Builds a request for creating or updating the <see cref="ImagingStudy"/> resource..
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The request entry.</returns>
        Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken);
    }
}
