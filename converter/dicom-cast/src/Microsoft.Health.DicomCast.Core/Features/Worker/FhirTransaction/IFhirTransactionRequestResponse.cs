// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides list of request/responses used to execute or returned by executing the FHIR transaction.
    /// </summary>
    /// <typeparam name="T">The type of the object used by the request or response.</typeparam>
    public interface IFhirTransactionRequestResponse<T>
    {
        /// <summary>
        /// Gets or sets the patient.
        /// </summary>
        T Patient { get; set; }

        /// <summary>
        /// Gets or sets the endpoint to DicomWeb used by ImagingStudy.
        /// </summary>
        T Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the imaging study.
        /// </summary>
        T ImagingStudy { get; set; }
    }
}
