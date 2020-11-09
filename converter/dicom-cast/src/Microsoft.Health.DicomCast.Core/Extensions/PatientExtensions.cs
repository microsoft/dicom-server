// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Patient"/>.
    /// </summary>
    public static class PatientExtensions
    {
        /// <summary>
        /// Compares two patients on the basis of the identifiers of name and gender.
        /// </summary>
        /// <param name="patient">This patient.</param>
        /// <param name="other">Patient to compare to.</param>
        /// <returns>True if identifiers are equivalent, false if they are not.</returns>
        public static bool IsEquivalent(this Patient patient, Patient other)
        {
            return patient.Name.IsExactly(other.Name) && patient.GenderElement.IsExactly(other.GenderElement);
        }

        /// <summary>
        /// Replaces values of properties on this patient with values on the other if they are not to be updated.
        /// </summary>
        /// <param name="patient">This patient.</param>
        /// <param name="other">Patient to use as baseline for 'static' properties.</param>
        public static void CleanPropertiesToNotUpdate(this Patient patient, Patient other)
        {
            patient.BirthDate = other.BirthDate;
        }
    }
}
