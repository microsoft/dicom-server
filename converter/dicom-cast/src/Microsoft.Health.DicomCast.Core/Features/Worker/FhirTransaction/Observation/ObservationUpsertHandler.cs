// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

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

    /// <inheritdoc/>
    class ObservationUpsertHandler : IObservationUpsertHandler
    {
        private readonly IFhirService _fhirService;

        public ObservationUpsertHandler(IFhirService fhirService)
        {
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));
            _fhirService = fhirService;
        }

        public async Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(context.Request, nameof(context.Request));

            IResourceId patientId = context.Request.Patient.ResourceId;
            IResourceId imagingStudyId = context.Request.ImagingStudy.ResourceId;
            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;

            // Parse all observations out of the dataset
            ParsedObservation parsedObservations = ObservationParser.CreateObservations(
                changeFeedEntry.Metadata,
                patientId.ToResourceReference(),
                imagingStudyId.ToResourceReference()
            );
            Collection<Observation> doseSummaries = parsedObservations.DoseSummaries;
            Collection<Observation> irradiationEvents = parsedObservations.IrradiationEvents;

            if (!doseSummaries.Any())
                return null;

            // operate on the pre-condition that their _should_ only be one DoseSummary per ImagingStudy.
            // If multiple observations are found; just use the first one.
            Observation doseSummary = doseSummaries[0];

            // Filter out existing observations
            Identifier imagingStudyIdentifier = imagingStudyId.ToResourceReference().Identifier;
            IEnumerable<Observation> existingDoseSummariesAsync = imagingStudyIdentifier != null
                ? await _fhirService
                    .RetrieveObservationsAsync(
                        imagingStudyId.ToResourceReference().Identifier,
                        cancellationToken)
                : new List<Observation>();

            var existingDoseSummaries =
                existingDoseSummariesAsync.ToList();
            var isExisting = existingDoseSummaries.Any();

            FhirTransactionRequestMode method = isExisting
                ? FhirTransactionRequestMode.Create
                : FhirTransactionRequestMode.Update;

            Bundle.RequestComponent request = isExisting
                ? new Bundle.RequestComponent()
                {
                    Method = Bundle.HTTPVerb.PUT,
                    Url = $"{ResourceType.Observation.GetLiteral()}/{existingDoseSummaries[0].Id}"
                }
                : new Bundle.RequestComponent()
                {
                    Method = Bundle.HTTPVerb.POST,
                    Url = ResourceType.Observation.GetLiteral()
                };

            FhirTransactionRequestMode transactionRequestMode = isExisting
                ? FhirTransactionRequestMode.Update
                : FhirTransactionRequestMode.Create;

            IResourceId resourceId = isExisting
                ? existingDoseSummaries[0].ToServerResourceId()
                : new ClientResourceId();

            var transactionRequestEntry = new FhirTransactionRequestEntry(
                transactionRequestMode,
                request,
                resourceId,
                doseSummary
            );

            // Even if there are multiple dose summary transactions, we only act on the first one.
            // There can _technically_ be multiple dose summary within a single report. However, it
            // goes against the dicom specification.
            // TODO figure out how we can support creation of irradiation events -- which require be able to create more than one instance of resource at once.
            return transactionRequestEntry;
        }
    }
}
