// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class PersonNameValidationTests
{
    [Theory]
    [InlineData("abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz")]
    [InlineData("abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz\0")]
    [InlineData("")]
    [InlineData(null)]
    public void GivenValidate_WhenValidatingNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomPersonName(DicomTag.PatientName, value);
        new PersonNameValidation().Validate(element);
    }

    [Fact]
    public void GivenMultipleValues_WhenValidating_ThenShouldValidateFirstOne()
    {
        DicomElement element = new DicomPersonName(DicomTag.PatientName, new string[] { "abc^xyz=abc^xyz^xyz^xyz^xyz=abc^xyz", "abc^efg^hij^pqr^lmn^xyz" });
        new PersonNameValidation().Validate(element);
    }

    [Theory]
    [InlineData("abc^xyz=abc^xyz=abc^xyz=abc^xyz", ValidationErrorCode.PersonNameExceedMaxGroups)] // too many groups (>3)
    [InlineData("abc^efg^hij^pqr^lmn^xyz", ValidationErrorCode.PersonNameExceedMaxComponents)]  // to many group components
    [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789", ValidationErrorCode.PersonNameGroupExceedMaxLength)]  // group is too long
    public void GivenInvalidPatientName_WhenValidating_ThenShouldThrow(string value, ValidationErrorCode errorCode)
    {
        DicomElement element = new DicomPersonName(DicomTag.PatientName, value);
        var ex = Assert.Throws<ElementValidationException>(() => new PersonNameValidation().Validate(element));
        Assert.Equal(errorCode, ex.ErrorCode);

    }

}
