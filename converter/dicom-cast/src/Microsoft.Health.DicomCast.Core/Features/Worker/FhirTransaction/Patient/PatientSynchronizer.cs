// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to <see cref="Patient"/> resource.
    /// </summary>
    public class PatientSynchronizer : IPatientSynchronizer
    {
        private readonly IEnumerable<IPatientPropertySynchronizer> _patientPropertySynchronizers;

        public PatientSynchronizer(IEnumerable<IPatientPropertySynchronizer> patientPropertySynchronizers)
        {
            EnsureArg.IsNotNull(patientPropertySynchronizers, nameof(patientPropertySynchronizers));

            _patientPropertySynchronizers = patientPropertySynchronizers;
        }

        /// <inheritdoc/>
        public void Synchronize(DicomDataset dataset, Patient patient)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(patient, nameof(patient));

            foreach (IPatientPropertySynchronizer patientPropertySynchronizer in _patientPropertySynchronizers)
            {
                patientPropertySynchronizer.Synchronize(dataset, patient);
            }
        }

        private static bool IsPatientUpdated(Patient patient, IDeepCopyable patientBeforeSynchronize)
        {
            // check if the patient property has changed after synchronization
            return !patient.IsExactly((IDeepComparable)patientBeforeSynchronize);
        }
    }
}
