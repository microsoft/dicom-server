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
    public class DicomLongStringValidationTests
    {

        [Fact]
        public void GivenValidateLongString_WhenValidating_ThenShouldPass()
        {
            new DicomLongStringValidation().Validate(new DicomLongString(DicomTag.WindowCenterWidthExplanation, "012345678912"));
        }

        [Theory]
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")] // exceed max length
        [InlineData("012\n")] // contains control character except Esc
        public void GivenInvalidLongString_WhenValidating_ThenShouldThrow(string value)
        {
            DicomElement element = new DicomLongString(DicomTag.WindowCenterWidthExplanation, value);
            Assert.Throws<DicomElementValidationException>(() => new DicomLongStringValidation().Validate(element));

        }

    }
}
