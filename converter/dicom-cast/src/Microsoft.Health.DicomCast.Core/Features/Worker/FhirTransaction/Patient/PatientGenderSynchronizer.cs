// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using FellowOakDicom;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to a specific <see cref="Patient.Gender"/> property.
    /// </summary>
    public class PatientGenderSynchronizer : IPatientPropertySynchronizer
    {
        private const string EmptyString = "";

        /// <inheritdoc/>
        public void Synchronize(DicomDataset dataset, Patient patient, bool isNewPatient)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(patient, nameof(patient));

            if (dataset.TryGetString(DicomTag.PatientSex, out string patientGender))
            {
                patient.Gender = patientGender switch
                {
                    "M" => AdministrativeGender.Male,
                    "F" => AdministrativeGender.Female,
                    "O" => AdministrativeGender.Other,
                    EmptyString => null,
                    _ => throw new InvalidDicomTagValueException(nameof(DicomTag.PatientSex), patientGender),
                };
            }
        }
    }
}
