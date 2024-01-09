// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class UidValidationTests
{

    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13.14.052")] // leading 0 is ok >> the 0 in front of the 5 in 0520 is considered a leading zero
    [InlineData("13.14.520\0")] // trailing null is ok
    [InlineData("13.14.520\0\0\0\0")] // multiple trailing nullsare ok
    [InlineData("13")] // single digit is ok
    [InlineData("0")] // single digit/segment that itself is a zero is ok
    [InlineData("0.2.4.6.8")] // starting with a zero for the uid is ok
    [InlineData("13.14.520.123")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("98.0.705.456.1.52365")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("123.0.45.6345.16765.0")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("12.0.0.678.324.145.123106.141.4905702.123480.9500026724.0.1.4020")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("12.0.0.678.324.145.123106.141.4905702.123480.9500026724.0.1.4020    ")] // empty str padding is ok
    public void GivenValidateUidWithLeniency_WhenValidating_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new UidValidation().Validate(element);
    }

    [Theory]
    [InlineData("")] // a uid that is a totally empty str is not ok
    [InlineData("\0")] // a uid that is just a null char and nothing else is not ok
    [InlineData(null)] // a uid that is null is not ok
    public void GivenValidateUidWithLeniency_WhenValidatingNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new UidValidation().Validate(element);
    }

    [Fact]
    public void GivenMultipleValuesWithLeniency_WhenValidating_ThenShouldValidateFirstOne()
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, "13", "11|");
        new UidValidation().Validate(element);
    }

    [Theory]
    [InlineData("13.14.520.")] // end with .
    [InlineData("13.14.5|20")] // | is invalid character
    [InlineData("12.1.1.678.324.145.123106.141.4905702.123480.9500026724.1.1.4020.12.1.1.678.324.145.123106.141.4905702.123480.9500026724.1.1.4020")] // value is too long
    [InlineData("13.14.5a20")] // segment itself invalid as it contains alpha char "a"
    [InlineData("13-14-520")] // segments should be separated by .
    [InlineData("\013.14.520")] // leading null padding is not ok
    [InlineData("13.\014.520")] // null padding in the middle of uid is not ok
    [InlineData("13.1\04.520")] // null padding in the middle of uid is not ok
    [InlineData("13.14\0.520")] // null padding in the middle of uid is not ok
    public void GivenInvalidUidWhenValidatingWithLeniency_ThenShouldThrow(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        Assert.Throws<InvalidIdentifierException>(() => new UidValidation().Validate(element));
    }

    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13")] // single digit is ok
    [InlineData("13.14.520.123")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("98.0.705.456.1.52365")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("123.1.45.6345.16765.1")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("12.1.1.678.324.145.123106.141.4905702.123480.9500026724.0.1.4020")] // just more examples of multiple segments with all expected chars and segmentation with periods
    [InlineData("12.1.1.678.324.145.123106.141.4905702.123480.9500026724.0.1.4020    ")] // empty str padding is ok
    public void GivenValidateUidWithStrictLevel_WhenValidating_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new UidValidation().Validate(element, ValidationLevel.Strict);
    }

    [Theory]
    [InlineData("")] // a uid that is a totally empty str is not ok
    [InlineData("\0")] // a uid that is just a null char and nothing else is not ok
    [InlineData(null)] // a uid that is null is not ok
    public void GivenValidateUidWithStrictLevel_WhenValidatingNullOrEmpty_ThenShouldNotPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        Assert.Throws<InvalidIdentifierException>(() => new UidValidation().Validate(element, ValidationLevel.Strict));
    }

    [Fact]
    public void GivenMultipleValuesWithStrictLevel_WhenValidating_ThenShouldValidateFirstOne()
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, "13", "11|");
        new UidValidation().Validate(element, ValidationLevel.Strict);
    }

    [Theory]
    [InlineData("13.14.520.")] // end with .
    [InlineData("13.14.5|20")] // | is invalid character
    [InlineData("12.1.1.678.324.145.123106.141.4905702.123480.9500026724.1.1.4020.12.1.1.678.324.145.123106.141.4905702.123480.9500026724.1.1.4020")] // value is too long
    [InlineData("13.14.5a20")] // segment itself invalid as it contains alpha char "a"
    [InlineData("13-14-520")] // segments should be separated by .
    [InlineData("\013.14.520")] // leading null padding is not ok
    [InlineData("13.\014.520")] // null padding in the middle of uid is not ok
    [InlineData("13.1\04.520")] // null padding in the middle of uid is not ok
    [InlineData("13.14\0.520")] // null padding in the middle of uid is not ok
    [InlineData("0")] // single digit/segment that itself is a zero is not ok
    [InlineData("1.012.4.6.8")] // starting segment with a zero for the uid is not ok
    [InlineData("1.0.4.6.8")] // when entire segment is a zero for the uid it is not ok
    [InlineData("13.14.520\0")] // trailing null is ok
    [InlineData("13.14.520\0\0\0\0")] // multiple trailing nulls are ok
    public void GivenInvalidUidWhenValidatingWithStrictLevel_ThenShouldThrow(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        Assert.Throws<InvalidIdentifierException>(() => new UidValidation().Validate(element, ValidationLevel.Strict));
    }

}
