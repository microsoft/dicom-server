// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Functions.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Extensions;

public class IAsyncEnumeratorExtensionsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2, 1, 2)]
    [InlineData(2, 1, 2, 3)]
    [InlineData(4, 1, 2, 3)]
    [InlineData(-1, 1, 2, 3)]
    public async Task GivenAsyncEnumerator_WhenTakingTopN_ThenReturnSubset(int top, params int[] elements)
    {
        int expected = Math.Min(Math.Max(0, top), elements.Length);
        IAsyncEnumerator<int> enumerator = elements.ToAsyncEnumerable().GetAsyncEnumerator();

        // First "half"
        int[] subset = await enumerator.Take(top).ToArrayAsync();
        Assert.Equal(expected, subset.Length);
        for (int i = 0; i < subset.Length; i++)
        {
            Assert.Equal(elements[i], subset[i]);
        }

        // Second "half"
        subset = await enumerator.Take(elements.Length).ToArrayAsync();
        Assert.Equal(elements.Length - expected, subset.Length);
        for (int i = 0; i < subset.Length; i++)
        {
            Assert.Equal(elements[expected + i], subset[i]);
        }
    }
}
