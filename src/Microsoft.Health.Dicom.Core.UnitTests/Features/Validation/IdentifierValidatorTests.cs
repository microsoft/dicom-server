// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class IdentifierValidatorTests
    {
        [Theory]
        [InlineData("1.01")]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000065")]
        public void GivenAnInvalidDicomId_WhenProcessingARequest_ThenAValidationMessageIsCreated(string id)
        {
            Assert.Throws<InvalidIdentifierException>(() => IdentifierValidator.ValidateAndThrow(id, nameof(id)));
        }

        [Theory]
        [InlineData("1.1")]
        [InlineData("")]
        [InlineData("59.88.90.100")]
        public void GivenAValidDicomId_WhenProcessingARequest_ThenAValidationMessageIsNotCreated(string id)
        {
            bool result = IdentifierValidator.Validate(id);
            Assert.True(result);
        }
    }
}
