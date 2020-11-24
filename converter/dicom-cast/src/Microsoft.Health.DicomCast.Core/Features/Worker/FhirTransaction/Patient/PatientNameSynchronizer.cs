// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Provides functionality to synchronize DICOM properties to a specific <see cref="Patient.Name"/> property.
    /// </summary>
    public class PatientNameSynchronizer : IPatientPropertySynchronizer
    {
        /// <inheritdoc/>
        public void Synchronize(DicomDataset dataset, Patient patient, bool isNewPatient)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(patient, nameof(patient));

            // Refer to PS3.5 6.2 and 6.2.1 for parsing logic.
            if (dataset.TryGetString(DicomTag.PatientName, out string patientName) && patientName != null)
            {
                // Find the existing name.
                HumanName name = patient.Name.FirstOrDefault(name => name.Use == HumanName.NameUse.Usual);

                if (name == null)
                {
                    name = new HumanName()
                    {
                        Use = HumanName.NameUse.Usual,
                    };

                    patient.Name.Add(name);
                }

                string[] parts = patientName.Trim(' ').Split('^');

                name.Family = parts[0];

                var combinedGivenNames = new List<string>();

                if (TryGetNamePart(1, out string[] givenNames))
                {
                    // Given name.
                    combinedGivenNames.AddRange(givenNames);
                }

                if (TryGetNamePart(2, out string[] middleNames))
                {
                    // Middle name.
                    combinedGivenNames.AddRange(middleNames);
                }

                if (TryGetNamePart(3, out string[] prefixes))
                {
                    // Prefix.
                    name.Prefix = prefixes;
                }

                if (TryGetNamePart(4, out string[] suffixes))
                {
                    // Suffix.
                    name.Suffix = suffixes;
                }

                name.Given = combinedGivenNames;

                bool TryGetNamePart(int index, out string[] nameParts)
                {
                    if (parts.Length > index && !string.IsNullOrWhiteSpace(parts[index]))
                    {
                        nameParts = parts[index].Split(' ');
                        return true;
                    }

                    nameParts = null;
                    return false;
                }
            }
        }
    }
}
