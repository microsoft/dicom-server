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
    public class LongStringValidationTests
    {

        [Fact]
        public void GivenValidateLongString_WhenValidating_ThenShouldPass()
        {
            new LongStringValidation().Validate(new DicomLongString(DicomTag.WindowCenterWidthExplanation, "012345678912"));
        }

        [Theory]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789", ValidationErrorCode.ExceedMaxLength)] // exceed max length
        [InlineData("012\n", ValidationErrorCode.InvalidCharacters)] // contains control character except Esc
        public void GivenInvalidLongString_WhenValidating_ThenShouldThrow(string value, ValidationErrorCode errorCode)
        {
            DicomElement element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, value);
            var ex = Assert.Throws<ElementValidationException>(() => new LongStringValidation().Validate(element));
            Assert.Equal(ex.ErrorCode, errorCode);
        }

    }
}
