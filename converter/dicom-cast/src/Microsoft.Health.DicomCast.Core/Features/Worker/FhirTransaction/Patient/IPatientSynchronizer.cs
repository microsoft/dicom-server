// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to <see cref="Patient"/> resource.
    /// </summary>
    public interface IPatientSynchronizer
    {
        /// <summary>
        /// Synchronizes the DICOM properties to <paramref name="patient"/>.
        /// </summary>
        /// <param name="context">The transaction context.</param>
        /// <param name="patient">The <see cref="Patient"/> resource.</param>
        /// <param name="isNewPatient">Flag to determine whether or not the patient being synchronized is new.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void Synchronize(FhirTransactionContext context, Patient patient, bool isNewPatient, CancellationToken cancellationToken = default);
    }
}
