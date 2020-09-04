// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class FhirTransactionRequestResponsePropertyAccessorTests
    {
        private readonly FhirTransactionRequest _fhirTransactionRequest = new FhirTransactionRequest();
        private readonly FhirTransactionResponse _fhirTransactionResponse = new FhirTransactionResponse();

        private readonly FhirTransactionRequestResponsePropertyAccessor _patientPropertyAccessor;

        public FhirTransactionRequestResponsePropertyAccessorTests()
        {
            _patientPropertyAccessor = CreatePropertyAccesor();
        }

        [Fact]
        public void GiveTheRequestEntryGetter_WhenInvoked_ThenCorrectValueShouldBeReturned()
        {
            FhirTransactionRequestEntry requestEntry = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Patient>();

            _fhirTransactionRequest.Patient = requestEntry;

            Assert.Same(
                requestEntry,
                _patientPropertyAccessor.RequestEntryGetter(_fhirTransactionRequest));
        }

        [Fact]
        public void GiveTheResponseEntryGetter_WhenInvoked_ThenCorrectValueShouldBeSet()
        {
            var responseEntry = new FhirTransactionResponseEntry(
                new Bundle.ResponseComponent(),
                new Patient());

            _patientPropertyAccessor.ResponseEntrySetter(_fhirTransactionResponse, responseEntry);

            Assert.Same(
                responseEntry,
                _fhirTransactionResponse.Patient);
        }

        [Fact]
        public void GivenSamePropertyAccessor_WhenHashCodeIsComputed_ThenHashCodeShouldBeTheSame()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccesor();

            Assert.Equal(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenPropertyAccessorWithDifferentPropertyName_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccesor(
                propertyName: "ImagingStudy");

            Assert.NotEqual(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenPropertyAccessorWithDifferentRequestEntryGetter_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccesor(
                requestEntryGetter: request => request.ImagingStudy);

            Assert.NotEqual(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenPropertyAccessorWithDifferentResponseEntrySetter_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccesor(
                responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry);

            Assert.NotEqual(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDefaultUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals((object)default));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToSamePropertyAccessorUsingObjectEquals_ThenTrueShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor.Equals((object)_patientPropertyAccessor));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenPropertyNameIsDifferentUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                (object)CreatePropertyAccesor(propertyName: "ImagingStudy")));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                (object)CreatePropertyAccesor(requestEntryGetter: request => request.ImagingStudy)));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                (object)CreatePropertyAccesor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry)));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDefaultUsingEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(default));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToSamePropertyAccessorUsingEquatableEquals_ThenTrueShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor.Equals(_patientPropertyAccessor));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenPropertyNameIsDifferentUsingEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                CreatePropertyAccesor(propertyName: "ImagingStudy")));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                CreatePropertyAccesor(requestEntryGetter: request => request.ImagingStudy)));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                CreatePropertyAccesor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry)));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDefaultUsingEqualityOperator_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor == default);
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToSamePropertyAccessorUsingEqualityOperator_ThenTrueShouldBeReturned()
        {
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(_patientPropertyAccessor == _patientPropertyAccessor);
#pragma warning restore CS1718 // Comparison made to same variable
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenPropertyNameIsDifferentUsingEqualityOperator_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor ==
                CreatePropertyAccesor(propertyName: "ImagingStudy"));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingEqualityOperator_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor ==
                CreatePropertyAccesor(requestEntryGetter: request => request.ImagingStudy));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingEqualityOperator_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor ==
                CreatePropertyAccesor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDefaultUsingInequalityOperator_ThenFalseShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor != default);
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToSamePropertyAccessorUsingInequalityOperator_ThenTrueShouldBeReturned()
        {
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.False(_patientPropertyAccessor != _patientPropertyAccessor);
#pragma warning restore CS1718 // Comparison made to same variable
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenPropertyNameIsDifferentUsingInequalityOperator_ThenFalseShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor !=
                CreatePropertyAccesor(propertyName: "ImagingStudy"));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingInequalityOperator_ThenFalseShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor !=
                CreatePropertyAccesor(requestEntryGetter: request => request.ImagingStudy));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingInequalityOperator_ThenFalseShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor !=
                CreatePropertyAccesor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry));
        }

        private FhirTransactionRequestResponsePropertyAccessor CreatePropertyAccesor(
            string propertyName = "Patient",
            Func<FhirTransactionRequest, FhirTransactionRequestEntry> requestEntryGetter = null,
            Action<FhirTransactionResponse, FhirTransactionResponseEntry> responseEntrySetter = null)
        {
            requestEntryGetter ??= request => request.Patient;
            responseEntrySetter ??= (response, responseEntry) => response.Patient = responseEntry;

            return new FhirTransactionRequestResponsePropertyAccessor(propertyName, requestEntryGetter, responseEntrySetter);
        }
    }
}
