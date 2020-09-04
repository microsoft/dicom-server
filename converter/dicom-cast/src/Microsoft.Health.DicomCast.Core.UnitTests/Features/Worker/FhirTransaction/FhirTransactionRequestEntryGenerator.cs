// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public static class FhirTransactionRequestEntryGenerator
    {
        public static FhirTransactionRequestEntry GenerateDefaultCreateRequestEntry<TResource>()
            where TResource : Resource, new()
        {
            return new FhirTransactionRequestEntry(
                FhirTransactionRequestMode.Create,
                new Bundle.RequestComponent()
                {
                    Method = Bundle.HTTPVerb.POST,
                },
                new ClientResourceId(),
                new TResource());
        }

        public static FhirTransactionRequestEntry GenerateDefaultUpdateRequestEntry<TResource>(ServerResourceId resourceId)
            where TResource : Resource, new()
        {
            return new FhirTransactionRequestEntry(
                FhirTransactionRequestMode.Update,
                new Bundle.RequestComponent()
                {
                    Method = Bundle.HTTPVerb.PUT,
                },
                resourceId,
                new TResource());
        }

        public static FhirTransactionRequestEntry GenerateDefaultNoChangeRequestEntry<TResource>(ServerResourceId resourceId)
            where TResource : Resource, new()
        {
            return new FhirTransactionRequestEntry(
                FhirTransactionRequestMode.None,
                request: null,
                resourceId,
                new TResource());
        }
    }
}
