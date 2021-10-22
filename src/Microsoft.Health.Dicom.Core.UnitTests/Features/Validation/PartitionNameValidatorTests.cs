// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class PartitionNameValidatorTests
    {
        [Theory]
        [InlineData("fooBAR")]
        [InlineData("fooBAR123")]
        [InlineData("fooBAR123.-_")]
        [InlineData("62f5c7eb-124a-49b1-9e5c-17c81a1a7137")]
        [InlineData("62f5c7eb_124a_49b1_9e5c-17c81a1a7137")]
        [InlineData("62f5c7eb.124a.49b1.9e5c.17c81a1a7137")]
        [InlineData("13.14.520")]
        [InlineData("1")]
        public void GivenValidPartitionId_WhenValidating_ThenShouldPass(string value)
        {
            PartitionNameValidator.Validate(value);
        }

        [Theory]
        [InlineData("")] // empty string
        [InlineData("123 ")] // has a space
        [InlineData("abc123@!")] // @ & ! are invalid characters
        [InlineData("62f5c7eb-124a-49b1-9e5c-17c81a1a7137/")] // / is invalid character
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789")] // value is too long
        public void GivenInValidPartitionId_WhenValidating_ThenShoulFail(string value)
        {
            Assert.Throws<InvalidPartitionNameException>(() => PartitionNameValidator.Validate(value));
        }

    }
}
