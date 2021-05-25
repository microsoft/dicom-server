// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.Fhir.Client;
using static Hl7.Fhir.Model.CapabilityStatement;
using IFhirClient = Microsoft.Health.Fhir.Client.IFhirClient;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Provides functionalities to communicate with FHIR server.
    /// </summary>
    public class FhirService : IFhirService
    {
        private readonly IFhirClient _fhirClient;
        private readonly IFhirResourceValidator _fhirResourceValidator;

        private readonly IEnumerable<FHIRVersion> _supportedFHIRVersions = new List<FHIRVersion> {FHIRVersion.N4_0_0, FHIRVersion.N4_0_1};

        public FhirService(IFhirClient fhirClient, IFhirResourceValidator fhirResourceValidator)
        {
            EnsureArg.IsNotNull(fhirClient, nameof(fhirClient));
            EnsureArg.IsNotNull(fhirResourceValidator, nameof(fhirResourceValidator));

            _fhirClient = fhirClient;
            _fhirResourceValidator = fhirResourceValidator;
        }

        /// <inheritdoc/>
        public Task<Patient> RetrievePatientAsync(Identifier identifier, CancellationToken cancellationToken)
            => SearchByIdentifierAsync<Patient>(identifier, cancellationToken);

        /// <inheritdoc/>
        public Task<ImagingStudy> RetrieveImagingStudyAsync(Identifier identifier, CancellationToken cancellationToken)
            => SearchByIdentifierAsync<ImagingStudy>(identifier, cancellationToken);

        /// <inheritdoc/>
        public Task<Endpoint> RetrieveEndpointAsync(string queryParameter, CancellationToken cancellationToken)
            => SearchByQueryParameterAsync<Endpoint>(queryParameter, cancellationToken);

        public Task<IEnumerable<Observation>> RetrieveObservationsAsync(Identifier identifier, CancellationToken cancellationToken = default)
        {
            return SearchMultiByIdentifierAsync<Observation>(identifier, cancellationToken);
        }

        public Task<Observation> RetrieveObservationAsync(Identifier imagingStudyID, CancellationToken cancellationToken)
            => SearchByIdentifierAsync<Observation>(imagingStudyID, cancellationToken);

        /// <inheritdoc/>
        public async Task CheckFhirServiceCapability(CancellationToken cancellationToken)
        {
            using FhirResponse<CapabilityStatement> response = await _fhirClient.ReadAsync<CapabilityStatement>("metadata", cancellationToken);
            FHIRVersion version = response.Resource.FhirVersion ?? throw new InvalidFhirServerException(DicomCastCoreResource.FailedToValidateFhirVersion);
            if (!_supportedFHIRVersions.Contains(version))
            {
                throw new InvalidFhirServerException(DicomCastCoreResource.InvalidFhirServerVersion);
            }

            foreach (RestComponent element in response.Resource.Rest)
            {
                foreach (SystemInteractionComponent interaction in element.Interaction)
                {
                    if (interaction.Code == SystemRestfulInteraction.Transaction)
                    {
                        return;
                    }
                }
            }

            throw new InvalidFhirServerException(DicomCastCoreResource.FhirServerTransactionNotSupported);
        }

        private async Task<TResource> SearchByIdentifierAsync<TResource>(Identifier identifier, CancellationToken cancellationToken)
            where TResource : Resource, new()
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            return await SearchByQueryParameterAsync<TResource>(identifier.ToSearchQueryParameter(), cancellationToken);
        }


        private async Task<IEnumerable<TResource>> SearchMultiByIdentifierAsync<TResource>(Identifier identifier, CancellationToken cancellationToken)
            where TResource : Resource, new()
        {
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            return await SearchMultiByQueryParameterAsync<TResource>(identifier.ToSearchQueryParameter(), cancellationToken);
        }

        private async Task<IEnumerable<TResource>> SearchMultiWithLimitByQueryParameterAsync<TResource>(string queryParameter, int maxCount, CancellationToken cancellationToken)
            where TResource : Resource, new()
        {
            EnsureArg.IsNotNullOrEmpty(queryParameter, nameof(queryParameter));
            string fhirTypeName = ModelInfo.GetFhirTypeNameForType(typeof(TResource));
            if (!Enum.TryParse(fhirTypeName, out ResourceType resourceType))
            {
                Debug.Assert(false, "Resource type could not be parsed from TResource");
            }

            Bundle bundle = await _fhirClient.SearchAsync(
                resourceType,
                queryParameter,
                count: null,
                cancellationToken);


            int matchCount = 0;
            var results = new List<TResource>();

            while (bundle != null)
            {
                matchCount += bundle.Entry.Count;
                if (matchCount > maxCount)
                {
                    // Too many matches
                    throw new MultipleMatchingResourcesException(typeof(TResource).Name);
                }

                IEnumerable<TResource> resources = bundle.Entry
                    .Select(component => (TResource)component.Resource);
                results.AddRange(resources);

                if (bundle.NextLink != null)
                {
                    bundle = await _fhirClient.SearchAsync(bundle.NextLink.ToString(), cancellationToken);
                }
                else
                {
                    break;
                }
            }

            // Validate to make sure the resources are valid.
            foreach (TResource resource in results)
            {
                _fhirResourceValidator.Validate(resource);
            }

            return results;
        }

        private async Task<IEnumerable<TResource>> SearchMultiByQueryParameterAsync<TResource>(string queryParameter, CancellationToken cancellationToken)
            where TResource : Resource, new()
        {
            return await SearchMultiWithLimitByQueryParameterAsync<TResource>(queryParameter, int.MaxValue, cancellationToken);
        }

        private async Task<TResource> SearchByQueryParameterAsync<TResource>(string queryParameter, CancellationToken cancellationToken)
            where TResource : Resource, new()
        {
            IEnumerable<TResource> matches = await SearchMultiWithLimitByQueryParameterAsync<TResource>(queryParameter, 1, cancellationToken);
            IEnumerable<TResource> enumerable = matches.ToList();
            return enumerable.Any() ? enumerable.First() : null;
        }
    }
}
