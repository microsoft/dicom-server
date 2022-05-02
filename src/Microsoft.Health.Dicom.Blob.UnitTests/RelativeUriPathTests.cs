// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests;

public class RelativeUriPathTests
{
    [Theory]
    [InlineData("first/second", "", "first/second")]
    [InlineData("", "third/forth", "third/forth")]
    [InlineData("first/second", "third", "first/second/third")]
    [InlineData("first/second/", "third", "first/second/third")]
    [InlineData("first/second", "/third", "first/second/third")]
    [InlineData("first/second/", "/third", "first/second/third")]
    public void GivenUriPaths_WhenCombining_ThenJoinProperly(string first, string second, string expected)
    {
        Assert.Equal(expected, RelativeUriPath.Combine(first, second));
    }
}
