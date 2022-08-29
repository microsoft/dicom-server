// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Functions.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Export;

public class ExportCheckpointTests
{
    [Fact]
    public void GivenCheckpoint_WhenRetrievingAdditionalProperties_ThenGetOperationSpecificValues()
    {
        var checkpoint = new ExportCheckpoint
        {
            ErrorHref = new Uri("http://storage-acccount/errors.json"),
            Progress = new ExportProgress(1234, 5),
        };

        ExportResults results = checkpoint.GetResults(null) as ExportResults;
        Assert.NotNull(results);
        Assert.Equal(1234, results.Exported);
        Assert.Equal(5, results.Skipped);
        Assert.Equal(new Uri("http://storage-acccount/errors.json"), results.ErrorHref);
    }
}
