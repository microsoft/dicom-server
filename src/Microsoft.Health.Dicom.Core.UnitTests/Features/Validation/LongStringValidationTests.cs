// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class LongStringValidationTests
{

    [Theory]
    [InlineData("")]
    [InlineData("012345678912")]
    [InlineData("012345678912\0")]
    [InlineData("\0")]
    [InlineData(null)]
    public void GivenValidate_WhenValidatingNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, value);
        new LongStringValidation().Validate(element);
    }

    [Fact]
    public void GivenMultipleValues_WhenValidating_ThenShouldValidateFirstOne()
    {
        var element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, "012345678912", "0123456789012345678901234567890123456789012345678901234567890123456789");
        new LongStringValidation().Validate(element);
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
