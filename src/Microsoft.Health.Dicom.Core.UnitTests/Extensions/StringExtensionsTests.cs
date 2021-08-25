// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("1", 1, "1")] // text.Length <= maxLength
        [InlineData("1", 0, "")] // text.Length > maxLength = 0
        [InlineData("123", 2, "12")] // text.Length > maxLength > 0 
        [InlineData("😁", 1, "")] // supplementary multilingual plane 
        public void GivenString_WhenTruncate_ThenShouldBeExpected(string text, int maxLength, string expected)
        {
            Assert.Equal(expected, text.Truncate(maxLength));
        }
    }
}
