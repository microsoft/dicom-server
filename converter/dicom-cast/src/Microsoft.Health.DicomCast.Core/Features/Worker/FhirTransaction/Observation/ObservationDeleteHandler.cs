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
    public class ObservationDeleteHandler : IObservationDeleteHandler
    {
        private readonly IFhirService _fhirService;

        public ObservationDeleteHandler(IFhirService fhirService)
        {
            _fhirService = EnsureArg.IsNotNull(fhirService, nameof(fhirService));
        }

        public async Task<IEnumerable<FhirTransactionRequestEntry>> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));

            Identifier identifier = IdentifierUtility.CreateIdentifier(context.ChangeFeedEntry.StudyInstanceUid);
            List<Observation> matchingObservations = (await _fhirService.RetrieveObservationsAsync(identifier, cancellationToken)).ToList();

            // terminate early if no observation found
            if (matchingObservations.Count == 0)
            {
                return null;
            }

            var requests = new List<FhirTransactionRequestEntry>();
            foreach (Observation observation in matchingObservations)
            {
                Bundle.RequestComponent request = new Bundle.RequestComponent()
                {
                    Method = Bundle.HTTPVerb.DELETE,
                    Url = $"{ResourceType.Observation.GetLiteral()}/{observation.Id}"
                };

                requests.Add(new FhirTransactionRequestEntry(
                    FhirTransactionRequestMode.Delete,
                    request,
                    observation.ToServerResourceId(),
                    observation));
            }

            return requests;
        }
    }
}
