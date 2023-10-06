// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class EncodedStringElementValidationTests
{
    private readonly EncodedStringElementValidation _validation = new();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("\0")]
    public void GivenValidate_WhenValidatingNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomTime(DicomTag.Time, value);
        _validation.Validate(element);
    }

    [Theory]
    [MemberData(nameof(ValidElements))]
    public void GivenValidDicomStringElement_WhenValidating_ThenPass(DicomElement element)
        => _validation.Validate(element);

    [Theory]
    [MemberData(nameof(InvalidElements))]
    public void GivenInvalidDicomStringElement_WhenValidating_ThenThrowElementValidationException(DicomElement element, ValidationErrorCode expectedError)
    {
        ElementValidationException exception = Assert.Throws<ElementValidationException>(() => _validation.Validate(element));
        Assert.Equal(expectedError, exception.ErrorCode);
        Assert.Contains(expectedError.GetMessage(), exception.Message);
    }


    [Fact]
    public void GivenDicomStringElementWithMultipleValues_WhenValidating_ThenShouldValidateFirstOne()
    {
        var element = new DicomTime(DicomTag.Time, DateTime.UtcNow.ToString("HHmmss'.'fffff", CultureInfo.InvariantCulture), "ABC");
        _validation.Validate(element);
    }

    public static IEnumerable<object[]> ValidElements = new object[][]
    {
        new object[] { new DicomDateTime(DicomTag.EffectiveDateTime, DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss'.'ffffff'+'0000", CultureInfo.InvariantCulture)) },
        new object[] { new DicomIntegerString(DicomTag.PixelAspectRatio, "0012345") },
        new object[] { new DicomTime(DicomTag.Time, DateTime.UtcNow.ToString("HHmmss'.'fffff", CultureInfo.InvariantCulture)) },
        new object[] { new DicomTime(DicomTag.Time, (string)null )},
    };

    public static object[][] InvalidElements = new object[][]
    {
        new object[] { new DicomDateTime(DicomTag.EffectiveDateTime, "6"), ValidationErrorCode.DateTimeIsInvalid },
        new object[] { new DicomIntegerString(DicomTag.PixelAspectRatio, "1234567890123"), ValidationErrorCode.IntegerStringIsInvalid },
        new object[] { new DicomIntegerString(DicomTag.PixelAspectRatio, "twelve"), ValidationErrorCode.IntegerStringIsInvalid },
        new object[] { new DicomTime(DicomTag.Time, "ABC"), ValidationErrorCode.TimeIsInvalid },
    };
}
