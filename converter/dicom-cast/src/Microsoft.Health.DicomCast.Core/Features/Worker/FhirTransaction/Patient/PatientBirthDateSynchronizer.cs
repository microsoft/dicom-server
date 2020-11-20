// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Extensions;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to a specific <see cref="Patient.BirthDate"/> property.
    /// </summary>
    public class PatientBirthDateSynchronizer : IPatientPropertySynchronizer
    {
        /// <inheritdoc/>
        public void Synchronize(DicomDataset dataset, Patient patient, FhirTransactionRequestMode requestMode)
        {
            if (requestMode.Equals(FhirTransactionRequestMode.Create))
            {
                EnsureArg.IsNotNull(dataset, nameof(dataset));
                EnsureArg.IsNotNull(patient, nameof(patient));

                FhirDateTime dicomPatientBirthDate = dataset.GetDatePropertyIfNotDefaultValue(DicomTag.PatientBirthDate);
                if (dicomPatientBirthDate != null)
                {
                    patient.BirthDate = dicomPatientBirthDate.ToString();
                }
            }
        }
    }
}
