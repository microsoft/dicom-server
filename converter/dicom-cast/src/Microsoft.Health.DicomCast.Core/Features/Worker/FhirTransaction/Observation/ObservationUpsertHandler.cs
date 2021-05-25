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
    /// <inheritdoc/>
    public class ObservationUpsertHandler : IObservationUpsertHandler
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
            {
                return null;
            }

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
            bool isExisting = existingDoseSummaries.Any();

            FhirTransactionRequestEntry transactionRequestEntry;
            if (isExisting)
            {
                transactionRequestEntry = new FhirTransactionRequestEntry(
                    FhirTransactionRequestMode.Update,
                    new Bundle.RequestComponent()
                    {
                        Method = Bundle.HTTPVerb.PUT,
                        Url = $"{ResourceType.Observation.GetLiteral()}/{existingDoseSummaries[0].Id}"
                    },
                    existingDoseSummaries[0].ToServerResourceId(),
                    doseSummary);
            }
            else
            {
                transactionRequestEntry = new FhirTransactionRequestEntry(
                    FhirTransactionRequestMode.Create,
                    new Bundle.RequestComponent()
                    {
                        Method = Bundle.HTTPVerb.POST,
                        Url = ResourceType.Observation.GetLiteral()
                    },
                    new ClientResourceId(),
                    doseSummary);
            }

            // Even if there are multiple dose summary transactions, we only act on the first one.
            // There can _technically_ be multiple dose summary within a single report. However, it
            // goes against the dicom specification.
            // TODO figure out how we can support creation of irradiation events -- which require be able to create more than one instance of resource at once.
            return transactionRequestEntry;
        }
    }
}
