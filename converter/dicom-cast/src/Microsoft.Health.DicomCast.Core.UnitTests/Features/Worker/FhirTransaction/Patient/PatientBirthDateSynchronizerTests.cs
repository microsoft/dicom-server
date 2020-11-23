// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class PatientBirthDateSynchronizerTests
    {
        private readonly PatientBirthDateSynchronizer _patientBirthDateSynchronizer = new PatientBirthDateSynchronizer();

        private readonly Patient _patient = new Patient();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenNoPatientBirthDate_WhenSynchronized_ThenNoBirthDateShouldBeAdded(bool newPatient)
        {
            _patientBirthDateSynchronizer.Synchronize(new DicomDataset(), _patient, newPatient);

            Assert.Null(_patient.BirthDate);
        }

        [Fact]
        public void GivenBirthDateWhileCreating_WhenSynchronized_ThenCorrectBirthDateShouldBeAdded()
        {
            DateTime birthDate = new DateTime(1990, 01, 01, 12, 12, 12);

            _patientBirthDateSynchronizer.Synchronize(CreateDicomDataset(birthDate), _patient, true);

            ValidateDate(birthDate, _patient.BirthDateElement);
        }

        [Fact]
        public void GivenBirthDateWithExistingPatient_WhenSynchronized_ThenNoBirthDateShouldBeAdded()
        {
            DateTime birthDate = new DateTime(1990, 01, 01, 12, 12, 12);

            _patientBirthDateSynchronizer.Synchronize(CreateDicomDataset(birthDate), _patient, false);

            Assert.Null(_patient.BirthDate);
        }

        private static DicomDataset CreateDicomDataset(DateTime patientBirthDate)
            => new DicomDataset()
            {
                { DicomTag.PatientBirthDate, patientBirthDate },
            };

        private static void ValidateDate(
            DateTime? expectedDate,
            Date actualDate)
        {
            if (!expectedDate.HasValue)
            {
                Assert.Null(actualDate);
                return;
            }

            Assert.NotNull(actualDate.ToDateTimeOffset());
            Assert.Equal(expectedDate.Value.Date, actualDate.ToDateTimeOffset().Value.DateTime);
        }
    }
}
