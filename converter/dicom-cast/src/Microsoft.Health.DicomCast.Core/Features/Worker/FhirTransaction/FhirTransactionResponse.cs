// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides list of responses returned by executing the FHIR transaction.
    /// </summary>
    public class FhirTransactionResponse : IFhirTransactionRequestResponse<FhirTransactionResponseEntry>
    {
        /// <inheritdoc/>
        public FhirTransactionResponseEntry Patient { get; set; }

        /// <inheritdoc/>
        public FhirTransactionResponseEntry Endpoint { get; set; }

        /// <inheritdoc/>
        public FhirTransactionResponseEntry ImagingStudy { get; set; }
    }
}
