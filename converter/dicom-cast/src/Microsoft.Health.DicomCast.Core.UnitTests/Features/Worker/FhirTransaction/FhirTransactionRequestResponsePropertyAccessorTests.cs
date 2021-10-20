// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
            _patientPropertyAccessor = CreatePropertyAccessor();
        }

        [Fact]
        public void GiveTheRequestEntryGetter_WhenInvoked_ThenCorrectValueShouldBeReturned()
        {
            FhirTransactionRequestEntry requestEntry = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Patient>();

            _fhirTransactionRequest.Patient = requestEntry;

            Assert.Same(
                requestEntry,
                _patientPropertyAccessor.RequestEntryGetter(_fhirTransactionRequest).Single());
        }

        [Fact]
        public void GiveTheResponseEntryGetter_WhenInvoked_ThenCorrectValueShouldBeSet()
        {
            FhirTransactionResponseEntry responseEntry = new(new Bundle.ResponseComponent(), new Patient());
            var responseEntryList = new List<FhirTransactionResponseEntry> { responseEntry };

            _patientPropertyAccessor.ResponseEntrySetter(_fhirTransactionResponse, responseEntryList);

            Assert.Same(
                responseEntry,
                _fhirTransactionResponse.Patient);
        }

        [Fact]
        public void GivenSamePropertyAccessor_WhenHashCodeIsComputed_ThenHashCodeShouldBeTheSame()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccessor();

            Assert.Equal(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenPropertyAccessorWithDifferentPropertyName_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccessor(
                propertyName: "ImagingStudy");

            Assert.NotEqual(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenPropertyAccessorWithDifferentRequestEntryGetter_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccessor(
                requestEntryGetter: request => new[] { request.ImagingStudy });

            Assert.NotEqual(_patientPropertyAccessor.GetHashCode(), anotherPatientPropertyAccessor.GetHashCode());
        }

        [Fact]
        public void GivenPropertyAccessorWithDifferentResponseEntrySetter_WhenHashCodeIsComputed_ThenHashCodeShouldBeDifferent()
        {
            FhirTransactionRequestResponsePropertyAccessor anotherPatientPropertyAccessor = CreatePropertyAccessor(
                responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry.Single());

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
                (object)CreatePropertyAccessor(propertyName: "ImagingStudy")));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                (object)CreatePropertyAccessor(requestEntryGetter: request => new[] { request.ImagingStudy })));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingObjectEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                (object)CreatePropertyAccessor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry.Single())));
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
                CreatePropertyAccessor(propertyName: "ImagingStudy")));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                CreatePropertyAccessor(requestEntryGetter: request => new[] { request.ImagingStudy })));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingEquatableEquals_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor.Equals(
                CreatePropertyAccessor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry.Single())));
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
                CreatePropertyAccessor(propertyName: "ImagingStudy"));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingEqualityOperator_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor ==
                CreatePropertyAccessor(requestEntryGetter: request => new[] { request.ImagingStudy }));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingEqualityOperator_ThenFalseShouldBeReturned()
        {
            Assert.False(_patientPropertyAccessor ==
                CreatePropertyAccessor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry.Single()));
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
                CreatePropertyAccessor(propertyName: "ImagingStudy"));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenRequestEntryGetterIsDifferentUsingInequalityOperator_ThenFalseShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor !=
                CreatePropertyAccessor(requestEntryGetter: request => new[] { request.ImagingStudy }));
        }

        [Fact]
        public void GivenAPropertyAccessor_WhenCheckingEqualToDifferentPropertyAccessorWhenResponseEntrySetterIsDifferentUsingInequalityOperator_ThenFalseShouldBeReturned()
        {
            Assert.True(_patientPropertyAccessor !=
                CreatePropertyAccessor(responseEntrySetter: (response, responseEntry) => response.ImagingStudy = responseEntry.Single()));
        }

        private FhirTransactionRequestResponsePropertyAccessor CreatePropertyAccessor(
            string propertyName = "Patient",
            Func<FhirTransactionRequest, IEnumerable<FhirTransactionRequestEntry>> requestEntryGetter = null,
            Action<FhirTransactionResponse, IEnumerable<FhirTransactionResponseEntry>> responseEntrySetter = null)
        {
            requestEntryGetter ??= request => new List<FhirTransactionRequestEntry> { request.Patient };
            responseEntrySetter ??= (response, responseEntry) => response.Patient = responseEntry.Single();

            return new FhirTransactionRequestResponsePropertyAccessor(propertyName, requestEntryGetter, responseEntrySetter);
        }
    }
}
