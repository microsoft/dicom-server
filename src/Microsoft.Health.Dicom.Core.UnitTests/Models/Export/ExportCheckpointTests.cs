// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Models.Export;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Models.Export;

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

        IReadOnlyDictionary<string, string> properties = checkpoint.AdditionalProperties;
        Assert.Equal(3, properties.Count);
        Assert.Equal("http://storage-acccount/errors.json", properties[nameof(ExportCheckpoint.ErrorHref)]);
        Assert.Equal("1234", properties[nameof(ExportProgress.Exported)]);
        Assert.Equal("5", properties[nameof(ExportProgress.Failed)]);
    }
}
