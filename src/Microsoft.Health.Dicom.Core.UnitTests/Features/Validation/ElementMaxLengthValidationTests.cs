// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class ElementMaxLengthValidationTests
{

    [Fact]
    public void GivenValueExceedMaxLength_WhenValidating_ThenShouldThrows()
    {
        var ex = Assert.Throws<ElementValidationException>(() =>
              new ElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, "0123456789121")));
        Assert.Equal(ValidationErrorCode.ExceedMaxLength, ex.ErrorCode);
    }

    [Theory]
    [InlineData("012345678912")]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData(null)]
    public void GivenValueInRange_WhenValidating_ThenShouldPass(string value)
    {
        new ElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, value));
    }

    [Fact]
    public void GivenMultipleValues_WhenValidating_ThenShouldValidateFirstOne()
    {
        // First one in range, second one out of range.
        new ElementMaxLengthValidation(12).Validate(new DicomIntegerString(DicomTag.DoseReferenceNumber, "012345678912", "0123456789121"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GivenValidate_WhenValidatingNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomIntegerString(DicomTag.DoseReferenceNumber, value);
        new ElementMaxLengthValidation(4).Validate(element);
    }
}
