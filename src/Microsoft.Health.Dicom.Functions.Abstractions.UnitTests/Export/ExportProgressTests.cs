// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Functions.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Export;

public class ExportProgressTests
{
    [Theory]
    [InlineData(0, 0, 1, 2)]
    [InlineData(1, 2, 3, 4)]
    public void GivenPartialProgress_WhenAdding_ThenAddPropertiesCorrectly(long success1, long skipped1, long success2, long skipped2)
    {
        var x = new ExportProgress(success1, skipped1);
        var y = new ExportProgress(success2, skipped2);

        ExportProgress actual = x + y;
        Assert.Equal(success1 + success2, actual.Exported);
        Assert.Equal(skipped1 + skipped2, actual.Skipped);
    }
}
