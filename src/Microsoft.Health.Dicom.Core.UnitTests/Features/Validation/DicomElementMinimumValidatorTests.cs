// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class DicomElementMinimumValidatorTests
    {
        [Theory]
        [InlineData("abc.123")]
        [InlineData("11|")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void GivenUIInvalidValue_WhenValidating_Throws(string id)
        {
            Assert.Throws<InvalidIdentifierException>(() => UidValidation.Validate(id, nameof(id)));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenCSInvalidValue_WhenValidating_Throws(string value)
        {
            DicomCodeString element = new DicomCodeString(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        [InlineData("abc\\efg")]
        public void GivenLOInvalidValue_WhenValidating_Throws(string value)
        {
            DicomLongString element = new DicomLongString(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("20100141")]
        [InlineData("233434343")]
        public void GivenDAInvalidValue_WhenValidating_Throws(string value)
        {
            DicomDate element = new DicomDate(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("0123456789abcdefg")]
        public void GivenSHInvalidValue_WhenValidating_Throws(string value)
        {
            DicomShortString element = new DicomShortString(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("abc^xyz=abc^xyz=abc^xyz=abc^xyz")]
        [InlineData("abc^efg^hij^pqr^lmn^xyz")]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void GivenPNInvalidValue_WhenValidating_Throws(string value)
        {
            DicomPersonName element = new DicomPersonName(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("01234567891234567")] // exceed max length
        public void GivenAEInvalidValue_WhenValidating_Throws(string value)
        {
            DicomApplicationEntity element = new DicomApplicationEntity(DicomTag.StudyInstanceUID, value);
            Assert.ThrowsAny<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("12345")] // exceed max length
        public void GivenASInvalidValue_WhenValidating_Throws(string value)
        {
            DicomAgeString element = new DicomAgeString(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }

        [Theory]
        [InlineData("0123456789123")] // exceed max length
        public void GivenISInvalidValue_WhenValidating_Throws(string value)
        {
            DicomIntegerString element = new DicomIntegerString(DicomTag.StudyInstanceUID, value);
            Assert.Throws<DicomStringElementValidationException>(() => new DicomElementMinimumValidator().Validate(element));
        }
    }
}
