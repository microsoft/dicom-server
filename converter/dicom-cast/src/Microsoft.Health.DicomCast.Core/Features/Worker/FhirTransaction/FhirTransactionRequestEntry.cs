// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides a FHIR transaction request detail.
    /// </summary>
    public class FhirTransactionRequestEntry
    {
        public FhirTransactionRequestEntry(
            FhirTransactionRequestMode requestMode,
            Bundle.RequestComponent request,
            IResourceId resourceId,
            Resource resource)
        {
            EnsureArg.EnumIsDefined(requestMode, nameof(requestMode));

            RequestMode = requestMode;
            Request = request;
            ResourceId = resourceId;
            Resource = resource;
        }

        /// <summary>
        /// Gets the request mode.
        /// </summary>
        public FhirTransactionRequestMode RequestMode { get; }

        /// <summary>
        /// Gets the request component.
        /// </summary>
        public Bundle.RequestComponent Request { get; }

        /// <summary>
        /// Gets the request resource id.
        /// </summary>
        public IResourceId ResourceId { get; }

        /// <summary>
        /// Gets the request resource.
        /// </summary>
        public Resource Resource { get; }
    }
}
