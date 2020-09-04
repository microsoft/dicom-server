// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Fhir
{
    public class FhirResourceValidatorTests
    {
        private readonly FhirResourceValidator _fhirResourceValidator = new FhirResourceValidator();

        [Fact]
        public void GivenAResourceMissingId_WhenValidated_ThenInvalidFhirResourceExceptionShouldBeThrown()
        {
            var patient = new Patient();

            Assert.Throws<FhirResourceValidationException>(() => _fhirResourceValidator.Validate(patient));
        }

        [Fact]
        public void GivenAResourceMissingMeta_WhenValidated_ThenInvalidFhirResourceExceptionShouldBeThrown()
        {
            var patient = new Patient()
            {
                Id = "p1",
                Meta = null,
            };

            Assert.Throws<FhirResourceValidationException>(() => _fhirResourceValidator.Validate(patient));
        }

        [Fact]
        public void GivenAResourceMissingVersionId_WhenValidated_ThenInvalidFhirResourceExceptionShouldBeThrown()
        {
            var patient = new Patient()
            {
                Id = "p1",
                Meta = new Meta(),
            };

            Assert.Throws<FhirResourceValidationException>(() => _fhirResourceValidator.Validate(patient));
        }

        [Fact]
        public void GivenAValidResource_WhenValidated_ThenItShouldNotThrowException()
        {
            var patient = new Patient()
            {
                Id = "p1",
                Meta = new Meta()
                {
                    VersionId = "1",
                },
            };

            _fhirResourceValidator.Validate(patient);
        }
    }
}
