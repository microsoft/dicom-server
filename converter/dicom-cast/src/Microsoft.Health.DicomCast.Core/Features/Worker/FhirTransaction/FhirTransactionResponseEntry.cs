// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides a FHIR transaction response detail.
    /// </summary>
    public class FhirTransactionResponseEntry
    {
        public FhirTransactionResponseEntry(
            Bundle.ResponseComponent response,
            Resource resource)
        {
            EnsureArg.IsNotNull(response, nameof(response));

            Response = response;
            Resource = resource;
        }

        /// <summary>
        /// Gets the response component.
        /// </summary>
        public Bundle.ResponseComponent Response { get; }

        /// <summary>
        /// Gets the response resource.
        /// </summary>
        public Resource Resource { get; }
    }
}
