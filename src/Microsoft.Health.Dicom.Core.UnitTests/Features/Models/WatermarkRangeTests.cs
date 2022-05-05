// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Features.Model;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Models;
public class WatermarkRangeTests
{

    [Fact]
    public void GivenSingleRange_WhenCombine_ThenReturnItself()
    {
        var expected = new WatermarkRange(1, 2);
        var range1 = new WatermarkRange(1, 2);
        Assert.Equal(expected, WatermarkRange.Combine(new[] { range1 }));
    }

    [Theory]
    [InlineData(1, 2, 3, 4, 1, 4)]
    [InlineData(3, 4, 1, 2, 1, 4)]
    [InlineData(1, 1, 2, 2, 1, 2)]
    [InlineData(1, 100, 101, 200, 1, 200)]
    public void GivenValidRange_WhenCombine_ThenReturnExpected(int start1, int end1, int start2, int end2, int expectedStart, int expectedEnd)
    {
        var expected = new WatermarkRange(expectedStart, expectedEnd);
        var range1 = new WatermarkRange(start1, end1);
        var range2 = new WatermarkRange(start2, end2);
        Assert.Equal(expected, range1.Combine(range2));
    }

    [Theory]
    [InlineData(1, 3, 5, 6)] // has gap
    [InlineData(1, 3, 2, 4)] // overlap
    public void GivenInvalidRanges_WhenCombine_ThenThrowException(int start1, int end1, int start2, int end2)
    {
        var range1 = new WatermarkRange(start1, end1);
        var range2 = new WatermarkRange(start2, end2);
        Assert.Throws<ArgumentException>(() => range1.Combine(range2));
    }

    [Fact]
    public void GivenAscendingBatches_WhenCombine_ThenShouldReturnExpected()
    {
        var range1 = new WatermarkRange(1, 3);
        var range2 = new WatermarkRange(4, 5);
        var range3 = new WatermarkRange(6, 8);
        Assert.Equal(new WatermarkRange(1, 8), WatermarkRange.Combine(new[] { range1, range2, range3 }));
    }

    [Fact]
    public void GivenDescendingBatches_WhenCombine_ThenShouldReturnExpected()
    {
        var range1 = new WatermarkRange(6, 8);
        var range2 = new WatermarkRange(4, 5);
        var range3 = new WatermarkRange(1, 3);
        Assert.Equal(new WatermarkRange(1, 8), WatermarkRange.Combine(new[] { range1, range2, range3 }));
    }
}
