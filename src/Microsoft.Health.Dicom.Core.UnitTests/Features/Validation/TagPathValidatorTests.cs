// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class TagPathValidatorTests
    {
        [InlineData("helloworld")]
        [InlineData("01012323")]
        [InlineData("0101,2323")]
        [InlineData("(01012323)")]
        [InlineData("(0101,2323))")]
        [InlineData("(0101,2323)-(0101,2323)")]
        [InlineData("(0101,2323).(0101,2323) (4545,5656)")]
        [Theory]
        public void GivenInvalidTagPath_WhenValidating_ThenValidationExceptionShouldBeThrown(string tagPath)
        {
            Assert.Throws<TagPathValidationException>(() => TagPathValidator.Validate(tagPath));
        }

        [InlineData(@"(0101,2323)")]
        [InlineData("(0101,2323).(0101,2323)")]
        [InlineData("(0101,2323).(0101,2323).(4545,5656)")]
        [Theory]
        public void GivenValidTagPath_WhenValidating_ThenShouldSucceed(string tagPath)
        {
            TagPathValidator.Validate(tagPath);
        }
    }
}
