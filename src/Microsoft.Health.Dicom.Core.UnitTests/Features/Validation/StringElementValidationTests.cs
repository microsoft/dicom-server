// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class StringElementValidationTests
{
    private class StringValidation : StringElementValidation
    {
        protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
        {
            if (value.Contains('\0'))
            {
                throw new Exception(value);
            }
        }
    }

    private class StringValidationNotAllowedNulls : StringElementValidation
    {
        protected override bool AllowNullOrEmpty => false;

        protected override void ValidateStringElement(string name, DicomVR vr, string value, IByteBuffer buffer)
        {
            if (string.IsNullOrEmpty(value) || value.Contains('\0'))
            {
                throw new Exception(value);
            }
        }
    }

    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13")]
    [InlineData("13\0\0\0")]
    [InlineData("\0\0\0")]
    [InlineData(null)]
    public void GivenAValue_WhenValidatingWithLeniencyAndAllowableNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new StringValidation().Validate(element);
    }

    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13")]
    [InlineData(null)]
    public void GivenAValue_WhenValidatingWithoutLeniencyAndAllowableNullOrEmpty_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new StringValidation().Validate(element, ValidationLevel.Strict);
    }

    [Theory]
    [InlineData("13\0\0\0")]
    [InlineData("\0\0\0")]
    public void GivenAValue_WhenValidatingWithoutLeniencyAndWithNullPadding_ThenShouldNotPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        Assert.Throws<Exception>(() => new StringValidation().Validate(element, ValidationLevel.Strict));
    }

    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13")]
    [InlineData("13\0\0\0")]
    public void GivenAValue_WhenValidatingWithLeniencyAndNullOrEmptyNotAllowed_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new StringValidationNotAllowedNulls().Validate(element);
    }

    [Theory]
    [InlineData("\0\0\0")]
    [InlineData(null)]
    public void GivenAValue_WhenValidatingWithLeniencyAndNullOrEmptyNotAllowed_ThenShouldNotPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        Assert.Throws<Exception>(() => new StringValidationNotAllowedNulls().Validate(element));
    }

    [Theory]
    [InlineData("13.14.520")]
    [InlineData("13")]
    public void GivenAValue_WhenValidatingWithoutLeniencyAndNullOrEmptyNotAllowed_ThenShouldPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        new StringValidationNotAllowedNulls().Validate(element, ValidationLevel.Strict);
    }

    [Theory]
    [InlineData("13\0\0\0")]
    [InlineData("\0\0\0")]
    [InlineData(null)]
    public void GivenAValue_WhenValidatingWithoutLeniencyAndNullOrEmptyNotAllowed_ThenShouldNotPass(string value)
    {
        DicomElement element = new DicomUniqueIdentifier(DicomTag.DigitalSignatureUID, value);
        Assert.Throws<Exception>(() => new StringValidationNotAllowedNulls().Validate(element, ValidationLevel.Strict));
    }

}