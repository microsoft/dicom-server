// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using FellowOakDicom.IO;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class ElementRequiredLengthValidationTests
{

    [Fact]
    public void GivenBinaryValueNotRequiredLength_WhenValidating_ThenShouldThrows()
    {
        DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, ByteConverter.ToByteBuffer(new byte[] { byte.MaxValue }));
        var ex = Assert.Throws<ElementValidationException>(() => new ElementRequiredLengthValidation(4).Validate(element));
        Assert.Equal(ValidationErrorCode.UnexpectedLength, ex.ErrorCode);
    }

    [Fact]
    public void GivenEmptyBinaryValue_WhenValidating_ThenShouldThrows()
    {
        DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, ByteConverter.ToByteBuffer(new byte[0]));
        var ex = Assert.Throws<ElementValidationException>(() => new ElementRequiredLengthValidation(4).Validate(element));
        Assert.Equal(ValidationErrorCode.UnexpectedLength, ex.ErrorCode);
    }

    [Fact]
    public void GivenBinaryValueOfRequiredLength_WhenValidating_ThenShouldPass()
    {
        DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, short.MaxValue);
        new ElementRequiredLengthValidation(2).Validate(element);
    }

    [Fact]
    public void GivenMultipleBinaryValues_WhenValidating_ThenShouldValidateFirstOne()
    {
        // First value if valid, second value is invalid
        DicomElement element = new DicomSignedShort(DicomTag.LargestImagePixelValue, ByteConverter.ToByteBuffer(new byte[] { 1, 2, 3 }));
        new ElementRequiredLengthValidation(2).Validate(element);
    }

    [Fact]
    public void GivenStringValueNotRequiredLength_WhenValidating_ThenShouldThrows()
    {
        DicomElement element = new DicomAgeString(DicomTag.PatientAge, "012W1");
        var ex = Assert.Throws<ElementValidationException>(() => new ElementRequiredLengthValidation(4).Validate(element));
        Assert.Equal(ValidationErrorCode.UnexpectedLength, ex.ErrorCode);
    }

    [Fact]
    public void GivenStringValueOfRequiredLength_WhenValidating_ThenShouldPass()
    {
        DicomElement element = new DicomAgeString(DicomTag.PatientAge, "012W");
        new ElementRequiredLengthValidation(4).Validate(element);
    }

    [Fact]
    public void GivenMultipleStringValues_WhenValidating_ThenShouldValidateFirstOne()
    {
        // First is valid, second is invalid
        DicomElement element = new DicomAgeString(DicomTag.PatientAge, "012W", "012W2");
        new ElementRequiredLengthValidation(4).Validate(element);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("", 1)]
    [InlineData(null, 0)]
    [InlineData(null, 1)]
    [InlineData("123\0", 4)]
    public void GivenValidate_WhenValidatingNullOrEmpty_ThenShouldNotPass(string value, int requiredLength)
    {
        DicomElement element = new DicomAgeString(DicomTag.PatientAge, value);
        Assert.Throws<ElementValidationException>(() => new ElementRequiredLengthValidation(requiredLength).Validate(element));
    }
}
