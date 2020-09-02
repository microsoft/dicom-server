// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Client.Models;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides the context of a FHIR transaction.
    /// </summary>
    public interface IFhirTransactionContext
    {
        /// <summary>
        /// Gets the change feed used for this transaction.
        /// </summary>
        ChangeFeedEntry ChangeFeedEntry { get; }

        /// <summary>
        /// Gets the request.
        /// </summary>
        FhirTransactionRequest Request { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        FhirTransactionResponse Response { get; }
    }
}
