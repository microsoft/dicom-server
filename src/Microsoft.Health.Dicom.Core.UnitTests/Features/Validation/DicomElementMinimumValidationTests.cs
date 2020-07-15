// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class DicomElementMinimumValidationTests
    {
        [Theory]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void GivenUIInvalidValue_WhenValidating_Throws(string id)
        {
            Assert.Throws<InvalidIdentifierException>(() => DicomElementMinimumValidation.ValidateUI(id, nameof(id)));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenCSInvalidValue_WhenValidating_Throws(string value)
        {
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateCS(value, nameof(value)));
        }

        [Theory]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        [InlineData("abc\\efg")]
        public void GivenLOInvalidValue_WhenValidating_Throws(string value)
        {
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateLO(value, nameof(value)));
        }

        [Theory]
        [InlineData("20100141")]
        [InlineData("233434343")]
        public void GivenDAInvalidValue_WhenValidating_Throws(string value)
        {
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateDA(value, nameof(value)));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenSHInvalidValue_WhenValidating_Throws(string value)
        {
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidateSH(value, nameof(value)));
        }

        [Theory]
        [InlineData("abc^xyz=abc^xyz=abc^xyz=abc^xyz")]
        [InlineData("abc^efg^hij^pqr^lmn^xyz")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void GivenPNInvalidValue_WhenValidating_Throws(string value)
        {
            Assert.Throws<DicomElementValidationException>(() => DicomElementMinimumValidation.ValidatePN(value, nameof(value)));
        }
    }
}
