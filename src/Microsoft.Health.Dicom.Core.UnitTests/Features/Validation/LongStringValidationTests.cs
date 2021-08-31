// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions.Validation;
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
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789", typeof(ExceedMaxLengthException))] // exceed max length
        [InlineData("012\n", typeof(InvalidCharactersException))] // contains control character except Esc
        public void GivenInvalidLongString_WhenValidating_ThenShouldThrow(string value, Type exceptionType)
        {
            DicomElement element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, value);
            Assert.Throws(exceptionType, () => new LongStringValidation().Validate(element));
        }

    }
}
