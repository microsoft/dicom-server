// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class PatientGenderSynchronizerTests
    {
        private readonly PatientGenderSynchronizer _patientGenderSynchronizer = new PatientGenderSynchronizer();

        [Theory]
        [InlineData("M", AdministrativeGender.Male, FhirTransactionRequestMode.Create)]
        [InlineData("M", AdministrativeGender.Male, FhirTransactionRequestMode.None)]
        [InlineData("F", AdministrativeGender.Female, FhirTransactionRequestMode.Create)]
        [InlineData("F", AdministrativeGender.Female, FhirTransactionRequestMode.None)]
        [InlineData("O", AdministrativeGender.Other, FhirTransactionRequestMode.Create)]
        [InlineData("O", AdministrativeGender.Other, FhirTransactionRequestMode.None)]
        [InlineData("", null, FhirTransactionRequestMode.Create)]
        [InlineData("", null, FhirTransactionRequestMode.None)]
        public void GivenAValidPatientSexTag_WhenSynchronized_ThenCorrectGenderShouldBeAssigned(string inputGender, AdministrativeGender? expectedGender, FhirTransactionRequestMode requestMode)
        {
            var dataset = new DicomDataset()
            {
                { DicomTag.PatientSex, inputGender },
            };

            var patient = new Patient();

            _patientGenderSynchronizer.Synchronize(dataset, patient, requestMode);

            Assert.Equal(expectedGender, patient.Gender);
        }

        [Fact]
        public void GivenAnInvalidPatientGender_WhenBuilt_ThenInvalidDicomTagValueExceptionShouldBeThrown()
        {
            var dataset = new DicomDataset()
            {
                { DicomTag.PatientSex, "D" },
            };

            Assert.Throws<InvalidDicomTagValueException>(() => _patientGenderSynchronizer.Synchronize(dataset, new Patient(), FhirTransactionRequestMode.Create));
        }
    }
}
