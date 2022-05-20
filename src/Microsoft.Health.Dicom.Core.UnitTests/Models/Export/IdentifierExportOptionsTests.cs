// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Models.Common;
using Microsoft.Health.Dicom.Core.Models.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Export;

public class IdentifierExportOptionsTests
{
    [Fact]
    public void GivenInvalidOptions_WhenValidating_ThenReturnResults()
    {
        Assert.Single(new IdentifierExportOptions { Values = null }.Validate(null));
        Assert.Single(new IdentifierExportOptions { Values = Array.Empty<DicomIdentifier>() }.Validate(null));
    }

    [Fact]
    public void GivenValidOptions_WhenValidating_ThenReturnNoFailures()
    {
        var options = new IdentifierExportOptions
        {
            Values = new List<DicomIdentifier>
            {
                DicomIdentifier.ForInstance("1.2", "3.4.5", "6.7.8.10"),
                DicomIdentifier.ForSeries("11.12.13", "14"),
                DicomIdentifier.ForStudy("1516.17"),
            },
        };

        Assert.Empty(options.Validate(null));
    }
}
