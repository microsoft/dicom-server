// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Models.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Export;

public class ExportProgressTests
{
    [Theory]
    [InlineData(0, 0, 1, 2)]
    [InlineData(1, 2, 3, 4)]
    public void GivenPartialProgress_WhenAdding_ThenAddPropertiesCorrectly(long success1, long failure1, long success2, long failure2)
    {
        var x = new ExportProgress(success1, failure1);
        var y = new ExportProgress(success2, failure2);

        ExportProgress actual = x + y;
        Assert.Equal(success1 + success2, actual.Exported);
        Assert.Equal(failure1 + failure2, actual.Failed);
        Assert.Equal(success1 + success2 + failure1 + failure2, actual.Total);
    }
}
