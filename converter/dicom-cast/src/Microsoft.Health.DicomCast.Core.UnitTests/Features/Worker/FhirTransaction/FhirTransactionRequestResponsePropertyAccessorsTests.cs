// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public class FhirTransactionRequestResponsePropertyAccessorsTests
    {
        private readonly FhirTransactionRequestResponsePropertyAccessors _transactionRequestResponsePropertyAccessors = new FhirTransactionRequestResponsePropertyAccessors();

        private readonly FhirTransactionRequest _fhirTransactionRequest = new FhirTransactionRequest()
        {
            Patient = FhirTransactionRequestEntryGenerator.GenerateDefaultCreateRequestEntry<Patient>(),
            ImagingStudy = FhirTransactionRequestEntryGenerator.GenerateDefaultUpdateRequestEntry<ImagingStudy>(new ServerResourceId(ResourceType.ImagingStudy, "123")),
            Endpoint = FhirTransactionRequestEntryGenerator.GenerateDefaultNoChangeRequestEntry<Endpoint>(new ServerResourceId(ResourceType.Endpoint, "abc")),
        };

        private readonly FhirTransactionResponse _fhirTransactionResponse = new FhirTransactionResponse();

        [Fact]
        public void GivenAPatientRequest_WhenPropertyGetterIsUsed_ThenCorrectValueShouldBeReturned()
        {
            ExecuteAndValidatePropertyGetter(nameof(FhirTransactionRequest.Patient), _fhirTransactionRequest.Patient);
        }

        [Fact]
        public void GivenAPatientResponse_WhenPropertySetterIsUsed_ThenCorrectValueShouldBeSet()
        {
            FhirTransactionResponseEntry expectedResponse = ExecutePropertySetter(nameof(FhirTransactionRequest.Patient));

            Assert.Same(_fhirTransactionResponse.Patient, expectedResponse);
            Assert.Null(_fhirTransactionResponse.ImagingStudy);
            Assert.Null(_fhirTransactionResponse.Endpoint);
        }

        [Fact]
        public void GivenAnImagingStudyRequest_WhenPropertyGetterIsUsed_ThenCorrectValueShouldBeReturned()
        {
            ExecuteAndValidatePropertyGetter(nameof(FhirTransactionRequest.ImagingStudy), _fhirTransactionRequest.ImagingStudy);
        }

        [Fact]
        public void GivenAnImagingStudyResponse_WhenPropertySetterIsUsed_ThenCorrectValueShouldBeSet()
        {
            FhirTransactionResponseEntry expectedResponse = ExecutePropertySetter(nameof(FhirTransactionRequest.ImagingStudy));

            Assert.Same(_fhirTransactionResponse.ImagingStudy, expectedResponse);
        }

        [Fact]
        public void GivenAnEndpointRequest_WhenPropertyGetterIsUsed_ThenCorrectValueShouldBeReturned()
        {
            ExecuteAndValidatePropertyGetter(nameof(FhirTransactionRequest.Endpoint), _fhirTransactionRequest.Endpoint);
        }

        [Fact]
        public void GivenAnEndpointResponse_WhenPropertySetterIsUsed_ThenCorrectValueShouldBeSet()
        {
            FhirTransactionResponseEntry expectedResponse = ExecutePropertySetter(nameof(FhirTransactionRequest.Endpoint));

            Assert.Same(_fhirTransactionResponse.Endpoint, expectedResponse);
        }

        private void ExecuteAndValidatePropertyGetter(string propertyName, FhirTransactionRequestEntry expectedEntry)
        {
            FhirTransactionRequestResponsePropertyAccessor propertyAccessor = GetPropertyAccessor(propertyName);

            FhirTransactionRequestEntry requestEntry = propertyAccessor.RequestEntryGetter(_fhirTransactionRequest);

            Assert.Same(expectedEntry, requestEntry);
        }

        private FhirTransactionResponseEntry ExecutePropertySetter(string propertyName)
        {
            FhirTransactionRequestResponsePropertyAccessor propertyAccessor = GetPropertyAccessor(propertyName);

            var expectedResponse = new FhirTransactionResponseEntry(new Bundle.ResponseComponent(), new Patient());

            propertyAccessor.ResponseEntrySetter(_fhirTransactionResponse, expectedResponse);

            return expectedResponse;
        }

        private FhirTransactionRequestResponsePropertyAccessor GetPropertyAccessor(string propertyName)
            => _transactionRequestResponsePropertyAccessors.PropertyAccessors.First(propertyAccessor => propertyAccessor.PropertyName == propertyName);
    }
}
