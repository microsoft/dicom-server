// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

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

    /// <inheritdoc/>
    class ObservationDeleteHandler : IObservationDeleteHandler
    {
        private readonly IFhirService _fhirService;

        public ObservationDeleteHandler(IFhirService fhirService)
        {
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));
            _fhirService = fhirService;
        }

        public async Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));

            // operate on the pre-condition that their _should_ only be one DoseSummary per ImagingStudy.
            // If multiple observations are found; just use the first one.
            // TODO current codebase only supports single resource per FhirTransactionRequest. Will need to refactor to support multiple.
            // TODO currently only supports deleting a single matched resource.
            Identifier identifier = ImagingStudyIdentifierUtility.CreateIdentifier(context.ChangeFeedEntry.StudyInstanceUid);
            IEnumerable<Observation> matchingObservationsAsync = await _fhirService.RetrieveObservationsAsync(identifier, cancellationToken);
            var matchingObservations = matchingObservationsAsync.ToList();

            // terminate early if no observation found
            if (!matchingObservations.Any())
                return null;

            Observation observationToDelete = matchingObservations.First();
            var request = new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.DELETE,
                Url = $"{ResourceType.Observation.GetLiteral()}/{observationToDelete.Id}"
            };

            return new FhirTransactionRequestEntry(
                FhirTransactionRequestMode.Delete,
                request,
                observationToDelete.ToServerResourceId(),
                observationToDelete);
        }
    }
}
