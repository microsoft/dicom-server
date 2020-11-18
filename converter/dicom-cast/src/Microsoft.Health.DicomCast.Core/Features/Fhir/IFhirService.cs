// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Fhir
{
    /// <summary>
    /// Provides functionalities to communicate with FHIR server.
    /// </summary>
    public interface IFhirService
    {
        /// <summary>
        /// Asynchronously retrieves an <see cref="Patient"/> resource from FHIR server matching the <paramref name="identifier"/>.
        /// </summary>
        /// <param name="identifier">The identifier of the patient.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the retrieving operation.</returns>
        Task<Patient> RetrievePatientAsync(Identifier identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves an <see cref="ImagingStudy"/> resource from FHIR server matching the <paramref name="identifier"/>.
        /// </summary>
        /// <param name="identifier">The identifier of the study.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the retrieving operation.</returns>
        Task<ImagingStudy> RetrieveImagingStudyAsync(Identifier identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves an <see cref="Endpoint"/> resource from FHIR server matching the <paramref name="queryParameter"/>.
        /// </summary>
        /// <param name="queryParameter">The queryparameter for endPoint.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the retrieving operation.</returns>
        Task<Endpoint> RetrieveEndpointAsync(string queryParameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that FHIR server is right version and supports transactions.
        /// </summary>
        void ValidateFhirService();
    }
}
