// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Dicom.Blob.Features.Export;
using Microsoft.Health.Dicom.Core.Features.Model;
using Xunit;

namespace Microsoft.Health.Dicom.Blob.UnitTests.Features.Export;

public class ExportFilePatternTests
{
    [Fact]
    public void GivenInvalidPatterns_WhenParsing_ThenThrow()
    {
        // Placeholders is internal, so we test the scenarios inline
        Assert.Throws<FormatException>(() => ExportFilePattern.Parse("foo\\", ExportPatternPlaceholders.Study));
        Assert.Throws<FormatException>(() => ExportFilePattern.Parse("bar\\baz", ExportPatternPlaceholders.Study));
        Assert.Throws<FormatException>(() => ExportFilePattern.Parse("%Operation%", ExportPatternPlaceholders.Study));
        Assert.Throws<FormatException>(() => ExportFilePattern.Parse("%HelloWorld%", ExportPatternPlaceholders.All));
    }

    [Fact]
    public void GivenValidPatterns_WhenParsing_ThenCreateFormatString()
    {
        // Placeholders is internal, so we test the scenarios inline
        Assert.Equal("foo/bar%baz", ExportFilePattern.Parse("foo/bar\\%baz", ExportPatternPlaceholders.None));
        Assert.Equal("{0:N}/{1}/other/{2}/{3}", ExportFilePattern.Parse("%OperATion%/%STudy%/other/%SerIES%/%SOPInstance%", ExportPatternPlaceholders.All));
    }

    [Fact]
    public void GivenOperationPattern_WhenFormatting_ThenReplacePattern()
    {
        var operationId = Guid.NewGuid();
        string format = ExportFilePattern.Parse("errors/%OperATion%/folder", ExportPatternPlaceholders.All);
        Assert.Equal(string.Format(CultureInfo.InvariantCulture, format, operationId), ExportFilePattern.Format(format, operationId));
    }

    [Fact]
    public void GivenCompletePattern_WhenFormatting_ThenReplacePattern()
    {
        var operationId = Guid.NewGuid();
        string format = ExportFilePattern.Parse("%OperATion%/%STudy%/other/%SerIES%/%SOPInstance%", ExportPatternPlaceholders.All);
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, format, operationId, "1", "2", "3"),
            ExportFilePattern.Format(format, operationId, new VersionedInstanceIdentifier("1", "2", "3", 1)));
    }
}
