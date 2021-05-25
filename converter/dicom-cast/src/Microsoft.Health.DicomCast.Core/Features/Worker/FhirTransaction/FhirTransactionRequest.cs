// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides list of request that will be used to execute the FHIR transaction.
    /// </summary>
    public class FhirTransactionRequest : IFhirTransactionRequestResponse<FhirTransactionRequestEntry>
    {
        /// <inheritdoc/>
        public FhirTransactionRequestEntry Patient { get; set; }

        /// <inheritdoc/>
        public FhirTransactionRequestEntry Endpoint { get; set; }

        /// <inheritdoc/>
        public FhirTransactionRequestEntry ImagingStudy { get; set; }

        /// <inheritdoc/>
        public FhirTransactionRequestEntry Observation { get; set; }
    }
}
