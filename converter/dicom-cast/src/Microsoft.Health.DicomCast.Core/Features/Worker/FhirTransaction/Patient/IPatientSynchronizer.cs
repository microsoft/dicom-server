// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
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
        /// <param name="dataset">The DICOM properties.</param>
        /// <param name="patient">The <see cref="Patient"/> resource.</param>
        void Synchronize(DicomDataset dataset, Patient patient);
    }
}
