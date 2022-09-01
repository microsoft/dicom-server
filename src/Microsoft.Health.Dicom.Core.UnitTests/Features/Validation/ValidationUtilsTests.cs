// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class ValidationUtilsTests
{

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("a\0")] // trailing null
    [InlineData("a\u001b")] // ESC
    public void GivenValidCharacters_WhenValidatingString_ThenShouldReturnTrue(string data)
    {
        Assert.True(ValidationUtils.ContainsValidStringCharacters(data));
    }

    [Theory]
    [InlineData("ab\0c^xyz=abc=abc^xyz")] // non-trailing null
    [InlineData("a\u0003bc^efg^hij^pqr^lmn^xyz")] // control character
    [InlineData("a\u0009b")] // formatting control character
    [InlineData("0123456\\789012345678901234567890123456789012345678901234567890123456789")] // slash
    public void GivenInvalidCharacters_WhenValidatingString_ThenShouldReturnFalse(string data)
    {
        Assert.False(ValidationUtils.ContainsValidStringCharacters(data));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("a\0")] // trailing null
    [InlineData("a\u001b")] // ESC
    [InlineData("a\u0009b")] // formatting control character
    [InlineData("0123456\\789012345678901234567890123456789012345678901234567890123456789")] // slash
    public void GivenValidCharacters_WhenValidatingTextString_ThenShouldReturnTrue(string data)
    {
        Assert.True(ValidationUtils.ContainsValidTextStringCharacters(data));
    }

    [Theory]
    [InlineData("ab\0c^xyz=abc=abc^xyz")] // non-trailing null
    [InlineData("a\u0003bc^efg^hij^pqr^lmn^xyz")] // control character
    public void GivenInvalidCharacters_WhenValidatingTextString_ThenShouldReturnFalse(string data)
    {
        Assert.False(ValidationUtils.ContainsValidTextStringCharacters(data));
    }
}
