// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Validation;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

public class UidValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GivenNullOrEmpty_WhenValidatingUidThatAllowsEmpty_ThenShouldPass(string value)
        => Assert.True(UidValidation.IsValid(value, allowEmpty: true));

    [Theory]
    [InlineData("0")]
    [InlineData("0.2.4.6.8")]
    [InlineData("98.0.705.456.1.52365")]
    [InlineData("123.0.45.6345.16765.0")]
    [InlineData("12.0.0.678.324.145.123106.141.4905702.123480.9500026724.0.1.4020")]
    [InlineData("12.0.0.678.324.145.123106.141.4905702.123480.9500026724.0.1.4020    ")]
    [InlineData("007")]
    [InlineData("12.003.456")]
    public void GivenValidString_WhenValidatingAsUid_ThenShouldPass(string value)
        => Assert.True(UidValidation.IsValid(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello.world")]
    [InlineData("987.in.valid.654")]
    [InlineData("24.foo.bar.baz")]
    [InlineData("98-0-705-456-1-52365")]
    [InlineData("123-0-45-6345-16765-0")]
    [InlineData("12.0.0.678.324.145.123106.141.4905702.123480.9500026724.0.1.40201")]
    public void GivenInvalidString_WhenValidatingAsUid_ThenShouldFail(string value)
        => Assert.False(UidValidation.IsValid(value));
}
