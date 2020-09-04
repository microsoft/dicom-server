// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class EndpointPipelineStep : IFhirTransactionPipelineStep
    {
        private readonly IFhirService _fhirService;
        private readonly string _dicomWebEndpoint;

        public EndpointPipelineStep(
            IOptions<DicomWebConfiguration> dicomWebConfiguration,
            IFhirService fhirService)
        {
            EnsureArg.IsNotNull(dicomWebConfiguration?.Value, nameof(dicomWebConfiguration));
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));

            _fhirService = fhirService;
            _dicomWebEndpoint = dicomWebConfiguration.Value.Endpoint.ToString();
        }

        public async Task PrepareRequestAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            string queryParameter = $"name={FhirTransactionConstants.EndpointName}&connection-type={FhirTransactionConstants.EndpointConnectionTypeSystem}|{FhirTransactionConstants.EndpointConnectionTypeCode}";

            Endpoint endpoint = await _fhirService.RetrieveEndpointAsync(queryParameter, cancellationToken);

            FhirTransactionRequestMode requestMode = FhirTransactionRequestMode.None;

            if (endpoint == null)
            {
                endpoint = new Endpoint()
                {
                    Name = FhirTransactionConstants.EndpointName,
                    Status = Endpoint.EndpointStatus.Active,
                    ConnectionType = new Coding()
                    {
                        System = FhirTransactionConstants.EndpointConnectionTypeSystem,
                        Code = FhirTransactionConstants.EndpointConnectionTypeCode,
                    },
                    Address = _dicomWebEndpoint,
                    PayloadType = new List<CodeableConcept>
                    {
                        new CodeableConcept(string.Empty, string.Empty, FhirTransactionConstants.EndpointPayloadTypeText),
                    },
                    PayloadMimeType = new string[]
                    {
                        FhirTransactionConstants.DicomMimeType,
                    },
                };

                requestMode = FhirTransactionRequestMode.Create;
            }
            else
            {
                // Make sure the address matches.
                if (!string.Equals(endpoint.Address, _dicomWebEndpoint, StringComparison.InvariantCulture))
                {
                    // We have found an endpoint with matching name and connection-type but the address does not match.
                    throw new FhirResourceValidationException(DicomCastCoreResource.MismatchEndpointAddress);
                }
            }

            Bundle.RequestComponent request = requestMode switch
            {
                FhirTransactionRequestMode.Create => new Bundle.RequestComponent()
                {
                    Method = Bundle.HTTPVerb.POST,
                    IfNoneExist = queryParameter,
                    Url = ResourceType.Endpoint.GetLiteral(),
                },
                _ => null,
            };

            IResourceId resourceId = requestMode switch
            {
                FhirTransactionRequestMode.Create => new ClientResourceId(),
                _ => endpoint.ToServerResourceId(),
            };

            context.Request.Endpoint = new FhirTransactionRequestEntry(
                requestMode,
                request,
                resourceId,
                endpoint);
        }

        public void ProcessResponse(FhirTransactionContext context)
        {
            // No action needed.
        }
    }
}
