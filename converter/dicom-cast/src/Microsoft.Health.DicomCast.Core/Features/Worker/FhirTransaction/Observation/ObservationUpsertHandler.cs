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
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ObservationUpsertHandler : IObservationUpsertHandler
    {
        private readonly IFhirService _fhirService;
        private readonly ObservationParser _observationParser;

        public ObservationUpsertHandler(IFhirService fhirService, ObservationParser observationParser)
        {
            _fhirService = EnsureArg.IsNotNull(fhirService, nameof(fhirService));
            _observationParser = EnsureArg.IsNotNull(observationParser, nameof(observationParser));
        }

        public async Task<IEnumerable<FhirTransactionRequestEntry>> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context?.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(context.Request, nameof(context.Request));

            IResourceId patientId = context.Request.Patient.ResourceId;
            IResourceId imagingStudyId = context.Request.ImagingStudy.ResourceId;
            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;

            Identifier identifier = IdentifierUtility.CreateIdentifier(changeFeedEntry.StudyInstanceUid);

            IEnumerable<Observation> observations = _observationParser.Parse(changeFeedEntry.Metadata, patientId.ToResourceReference(), imagingStudyId.ToResourceReference(), identifier);

            if (!observations.Any())
            {
                return Enumerable.Empty<FhirTransactionRequestEntry>();
            }

            Identifier imagingStudyIdentifier = imagingStudyId.ToResourceReference().Identifier;
            IEnumerable<Observation> existingDoseSummariesAsync = imagingStudyIdentifier != null
                ? await _fhirService
                    .RetrieveObservationsAsync(
                        imagingStudyId.ToResourceReference().Identifier,
                        cancellationToken)
                : new List<Observation>();

            List<FhirTransactionRequestEntry> fhirRequests = new List<FhirTransactionRequestEntry>();
            foreach (var observation in observations)
            {
                fhirRequests.Add(new FhirTransactionRequestEntry(
                    FhirTransactionRequestMode.Create,
                    new Bundle.RequestComponent()
                    {
                        Method = Bundle.HTTPVerb.POST,
                        Url = ResourceType.Observation.GetLiteral()
                    },
                    new ClientResourceId(),
                    observation));
            }

            return fhirRequests;
        }
    }
}
