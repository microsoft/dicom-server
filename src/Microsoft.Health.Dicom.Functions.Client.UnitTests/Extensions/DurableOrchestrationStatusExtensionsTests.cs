// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Extensions;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.Extensions;

public class DurableOrchestrationStatusExtensionsTests
{
    [Theory]
    [InlineData(null, DicomOperation.Unknown)]
    [InlineData("foo", DicomOperation.Unknown)]
    [InlineData("Unknown", DicomOperation.Unknown)]
    [InlineData(FunctionNames.ReindexInstances, DicomOperation.Reindex)]
    [InlineData(FunctionNames.ExportDicomFiles, DicomOperation.Export)]
    [InlineData("reindexINSTANCESasync", DicomOperation.Reindex)]
    public void GivenOrchestrationStatus_WhenGettingDicomOperation_ThenConvertNameToType(string name, DicomOperation expected)
    {
        Assert.Equal(expected, new DurableOrchestrationStatus { Name = name }.GetDicomOperation());
    }
}
