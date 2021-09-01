// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Exceptions;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Exceptions
{
    public class InvalidIdentifierExceptionTests
    {
        [Fact]
        public void GivenInvalidIdentifierException_WhenGetMessage_ShouldReturnExpected()
        {
            var name = "tagname";
            var value = "tagvalue";
            var exception = new InvalidIdentifierException(name, value);
            Assert.Equal($"Dicom element '{name}' with value '{value}' failed validation for VR 'UI': DICOM Identifier is invalid. Value length should not exceed the maximum length of 64 characters. Value should contain characters in '0'-'9' and '.'. Each component must start with non-zero number.", exception.Message);
        }
    }
}
