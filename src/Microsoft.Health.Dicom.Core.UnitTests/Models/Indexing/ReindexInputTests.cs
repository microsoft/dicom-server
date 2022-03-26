// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Models.Indexing;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Indexing;

public class ReindexInputTests
{
    [Fact]
    public void GivenEmptyInput_WhenGettingPercentComplete_ThenReturnZero()
        => Assert.Equal(0, new ReindexInput().PercentComplete);

    [Fact]
    public void GivenEmptyInput_WhenGettingResourceIds_ThenReturnZero()
        => Assert.Null(new ReindexInput().ResourceIds);

    [Theory]
    [InlineData(4, 4, 25)]
    [InlineData(3, 4, 50)]
    [InlineData(2, 4, 75)]
    [InlineData(1, 4, 100)]
    [InlineData(1, 1, 100)]
    public void GivenReindexInput_WhenGettingPercentComplete_ThenReturnComputedProgress(int start, int end, int expected)
        => Assert.Equal(expected, new ReindexInput { Completed = new WatermarkRange(start, end) }.PercentComplete);

    [Fact]
    public void GivenReindexInput_WhenGettingResourceIds_ThenReturnConvertedIds()
    {
        int[] expectedTagKeys = new int[] { 1, 3, 10 };
        var input = new ReindexInput { QueryTagKeys = expectedTagKeys };
        Assert.True(input.ResourceIds.SequenceEqual(expectedTagKeys.Select(x => x.ToString(CultureInfo.InvariantCulture))));
    }
}
