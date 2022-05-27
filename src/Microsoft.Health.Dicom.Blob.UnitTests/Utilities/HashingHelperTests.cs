// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Blob.Utilities;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Utilities;

public class HashingHelperTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(123L)]
    [InlineData(-85738)]
    public void GivenLongs_WhenHashing_ThenReturnDifferentValues(long value)
    {
        int hash = HashingHelper.GetXxHashCode(value);
        Assert.NotEqual((int)value, hash);
        Assert.NotEqual(HashingHelper.GetXxHashCode(99), hash);
    }
}
