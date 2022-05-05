// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Export;

public class IdentifierExportOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1/2/3", "foo")]
    [InlineData("1/bar")]

    public void GivenInvalidOptions_WhenValidating_ThenReturnResults(params string[] values)
    {
        var options = new IdentifierExportOptions
        {
            // Use empty to indicate an empty array
            Values = values?.Length == 1 && values[0].Length == 0 ? Array.Empty<string>() : values
        };

        Assert.Single(options.Validate(null));
    }

    [Fact]
    public void GivenValidOptions_WhenValidating_ThenReturnNoFailures()
    {
        var options = new IdentifierExportOptions
        {
            Values = new List<string>
            {
                "1.2/3.4.5/6.7.8.10",
                "11.12.13/14",
                "1516.17",
            },
        };

        Assert.Empty(options.Validate(null));
    }
}
