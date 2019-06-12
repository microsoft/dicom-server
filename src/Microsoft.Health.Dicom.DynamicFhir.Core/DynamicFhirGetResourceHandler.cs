// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Get;

namespace Microsoft.Health.Dicom.DynamicFhir.Core
{
    public class DynamicFhirGetResourceHandler : IRequestHandler<GetResourceRequest, GetResourceResponse>
    {
        private readonly IFhirDataStore _fhirDataStore;
        private readonly ResourceDeserializer _deserializer;

        public DynamicFhirGetResourceHandler(
            IFhirDataStore fhirDataStore,
            ResourceDeserializer deserializer)
        {
            EnsureArg.IsNotNull(fhirDataStore, nameof(fhirDataStore));
            EnsureArg.IsNotNull(deserializer, nameof(deserializer));

            _fhirDataStore = fhirDataStore;
            _deserializer = deserializer;
        }

        /// <summary>
        /// This is a duplicate of Microsoft.Health.Fhir.Core.Resources.ResourceNotFoundById,
        /// this is internal in the current fhir-server code base.
        /// </summary>
        private static string ResourceNotFoundById
        {
            get { return "Resource type '{0}' with id '{1}' couldn't be found."; }
        }

        public async Task<GetResourceResponse> Handle(GetResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            var key = message.ResourceKey;

            ResourceWrapper currentDoc = await _fhirDataStore.GetAsync(key, cancellationToken);

            if (currentDoc == null)
            {
                throw new ResourceNotFoundException(string.Format(ResourceNotFoundById, key.ResourceType, key.Id));
            }

            return new GetResourceResponse(_deserializer.Deserialize(currentDoc));
        }
    }
}
