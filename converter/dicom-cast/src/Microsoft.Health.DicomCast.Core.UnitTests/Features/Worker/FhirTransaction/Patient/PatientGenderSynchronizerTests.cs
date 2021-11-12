// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class PatientGenderSynchronizerTests
    {
        private readonly PatientGenderSynchronizer _patientGenderSynchronizer = new PatientGenderSynchronizer();

        [Theory]
        [InlineData("M", AdministrativeGender.Male, true)]
        [InlineData("M", AdministrativeGender.Male, false)]
        [InlineData("F", AdministrativeGender.Female, true)]
        [InlineData("F", AdministrativeGender.Female, false)]
        [InlineData("O", AdministrativeGender.Other, true)]
        [InlineData("O", AdministrativeGender.Other, false)]
        [InlineData("", null, true)]
        [InlineData("", null, false)]
        public void GivenAValidPatientSexTag_WhenSynchronized_ThenCorrectGenderShouldBeAssigned(string inputGender, AdministrativeGender? expectedGender, bool isNewPatient)
        {
            var dataset = new DicomDataset()
            {
                { DicomTag.PatientSex, inputGender },
            };

            var patient = new Patient();

            _patientGenderSynchronizer.Synchronize(dataset, patient, isNewPatient);

            Assert.Equal(expectedGender, patient.Gender);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenAnInvalidPatientGender_WhenBuilt_ThenInvalidDicomTagValueExceptionShouldBeThrown(bool isNewPatient)
        {
            var dataset = new DicomDataset()
            {
                { DicomTag.PatientSex, "D" },
            };

            Assert.Throws<InvalidDicomTagValueException>(() => _patientGenderSynchronizer.Synchronize(dataset, new Patient(), isNewPatient));
        }
    }
}
