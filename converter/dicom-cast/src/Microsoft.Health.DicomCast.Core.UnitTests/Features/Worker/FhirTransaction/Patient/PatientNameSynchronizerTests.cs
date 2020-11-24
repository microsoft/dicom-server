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
    public class PatientNameSynchronizerTests
    {
        private readonly PatientNameSynchronizer _patientNameSynchronizer = new PatientNameSynchronizer();

        private readonly Patient _patient = new Patient();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenNoPatientName_WhenSynchronized_ThenNoNameShouldBeAdded(bool isNewPatient)
        {
            _patientNameSynchronizer.Synchronize(new DicomDataset(), _patient, isNewPatient);

            Assert.Empty(_patient.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenFamilyName_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient)
        {
            const string familyName = "fn";

            _patientNameSynchronizer.Synchronize(CreateDicomDataset(familyName), _patient, isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(name, expectedFamilyName: familyName));
        }

        [Theory]
        [InlineData(true, "^")]
        [InlineData(false, "^")]
        [InlineData(true, "^gn", "gn")]
        [InlineData(false, "^gn", "gn")]
        [InlineData(true, "^gn1 gn2", "gn1", "gn2")]
        [InlineData(false, "^gn1 gn2", "gn1", "gn2")]
        public void GivenGivenNames_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient, string inputName, params string[] givenNames)
        {
            _patientNameSynchronizer.Synchronize(CreateDicomDataset(inputName), _patient, isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(name, expectedGivenNames: givenNames));
        }

        [Theory]
        [InlineData(true, "^^")]
        [InlineData(false, "^^")]
        [InlineData(true, "^^mn", "mn")]
        [InlineData(false, "^^mn", "mn")]
        [InlineData(true, "^^mn1 mn2", "mn1", "mn2")]
        [InlineData(false, "^^mn1 mn2", "mn1", "mn2")]
        public void GivenMiddleNames_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient, string inputName, params string[] givenNames)
        {
            _patientNameSynchronizer.Synchronize(CreateDicomDataset(inputName), _patient, isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(name, expectedGivenNames: givenNames));
        }

        [Theory]
        [InlineData(true, "^^^")]
        [InlineData(false, "^^^")]
        [InlineData(true, "^^^p1", "p1")]
        [InlineData(false, "^^^p1", "p1")]
        [InlineData(true, "^^^p1 p2", "p1", "p2")]
        [InlineData(false, "^^^p1 p2", "p1", "p2")]
        public void GivenPrefixes_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient, string inputName, params string[] prefixes)
        {
            _patientNameSynchronizer.Synchronize(CreateDicomDataset(inputName), _patient, isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(name, expectedPrefixes: prefixes));
        }

        [Theory]
        [InlineData(true, "^^^^")]
        [InlineData(false, "^^^^")]
        [InlineData(true, "^^^^s1", "s1")]
        [InlineData(false, "^^^^s1", "s1")]
        [InlineData(true, "^^^^s1 s2", "s1", "s2")]
        [InlineData(false, "^^^^s1 s2", "s1", "s2")]
        public void GivenSuffixes_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient, string inputName, params string[] suffixes)
        {
            _patientNameSynchronizer.Synchronize(CreateDicomDataset(inputName), _patient, isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(name, expectedSuffixes: suffixes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenNameWithSpacePadding_WhenSynchronized_ThenPaddingShouldBeRemoved(bool isNewPatient)
        {
            _patientNameSynchronizer.Synchronize(
                CreateDicomDataset(" Doe^Joe    "),
                _patient,
                isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(
                    name,
                    expectedFamilyName: "Doe",
                    expectedGivenNames: new[] { "Joe" }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenName_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient)
        {
            _patientNameSynchronizer.Synchronize(
                CreateDicomDataset("Adams^John Robert^Quincy^Rev.^B.A. M.Div."),
                _patient,
                isNewPatient);

            Assert.Collection(
                _patient.Name,
                name => ValidateName(
                    name,
                    expectedFamilyName: "Adams",
                    expectedGivenNames: new[] { "John", "Robert", "Quincy" },
                    expectedPrefixes: new[] { "Rev." },
                    expectedSuffixes: new[] { "B.A.", "M.Div." }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenNameAlreadyExists_WhenSynchronized_ThenItWillBeOverwritten(bool isNewPatient)
        {
            _patient.Name.Add(new HumanName()
            {
                Use = HumanName.NameUse.Usual,
                Family = "Smith",
                Given = new[] { "John" },
            });

            _patientNameSynchronizer.Synchronize(
                CreateDicomDataset("Morrison-Jones^Susan^^^Ph.D., Chief Executive Officer"),
                _patient,
                isNewPatient);

            // The spec says the suffix should be two, but I am not sure how we can do that without
            // some sort of natural language interpretation. For now, because we are separating by space,
            // the "Chief Executive Officer" will be split into 3 different suffixes.
            Assert.Collection(
                _patient.Name,
                name => ValidateName(
                    name,
                    expectedFamilyName: "Morrison-Jones",
                    expectedGivenNames: new[] { "Susan" },
                    expectedSuffixes: new[] { "Ph.D.,", "Chief", "Executive", "Officer" }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenOtherName_WhenSynchronized_ThenCorrectNameShouldBeAdded(bool isNewPatient)
        {
            _patient.Name.Add(new HumanName()
            {
                Use = HumanName.NameUse.Usual,
                Family = "Smith",
                Given = new[] { "John" },
            });

            _patient.Name.Add(new HumanName()
            {
                Use = HumanName.NameUse.Official,
                Family = "Smith",
                Given = new[] { "John" },
            });

            _patientNameSynchronizer.Synchronize(
                CreateDicomDataset("Schmith^Johnny"),
                _patient,
                isNewPatient);

            // The spec says the suffix should be two, but I am not sure how we can do that without
            // some sort of natural language interpretation. For now, because we are separating by space,
            // the "Chief Executive Officer" will be split into 3 different suffixes.
            Assert.Collection(
                _patient.Name,
                name => ValidateName(
                    name,
                    expectedFamilyName: "Schmith",
                    expectedGivenNames: new[] { "Johnny" }),
                name => ValidateName(
                    name,
                    expectedUse: HumanName.NameUse.Official,
                    expectedFamilyName: "Smith",
                    expectedGivenNames: new[] { "John" }));
        }

        private static DicomDataset CreateDicomDataset(string patientName)
            => new DicomDataset()
            {
                { DicomTag.PatientName, patientName },
            };

        private static void ValidateName(
            HumanName actualName,
            HumanName.NameUse expectedUse = HumanName.NameUse.Usual,
            string expectedFamilyName = "",
            string[] expectedGivenNames = null,
            string[] expectedPrefixes = null,
            string[] expectedSuffixes = null)
        {
            if (expectedGivenNames == null)
            {
                expectedGivenNames = Array.Empty<string>();
            }

            if (expectedPrefixes == null)
            {
                expectedPrefixes = Array.Empty<string>();
            }

            if (expectedSuffixes == null)
            {
                expectedSuffixes = Array.Empty<string>();
            }

            Assert.NotNull(actualName);
            Assert.Equal(expectedUse, actualName.Use);
            Assert.Equal(expectedFamilyName, actualName.Family);
            Assert.Equal(expectedGivenNames, actualName.Given);
            Assert.Equal(expectedPrefixes, actualName.Prefix);
            Assert.Equal(expectedSuffixes, actualName.Suffix);
        }
    }
}
