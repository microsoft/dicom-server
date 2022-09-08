// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Client.Extensions;
using Microsoft.Health.Operations;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.Client.UnitTests.Extensions;

public class OperationQueryConditionExtensionsTests
{
    [Theory]
    [InlineData(OperationStatus.Unknown)]
    [InlineData((OperationStatus)42)]
    public void GivenInvalidStatus_WhenGettingRuntimeStatus_ThenThrow(OperationStatus status)
    {
        var query = new OperationQueryCondition<DicomOperation> { Statuses = new OperationStatus[] { status } };
        Assert.Throws<ArgumentOutOfRangeException>(() => query.ForDurableFunctions());
    }

    [Theory]
    [InlineData(OperationStatus.Canceled, OrchestrationRuntimeStatus.Canceled, OrchestrationRuntimeStatus.Terminated)]
#pragma warning disable CS0618
    [InlineData(OperationStatus.Completed, OrchestrationRuntimeStatus.Completed)]
#pragma warning restore CS0618
    [InlineData(OperationStatus.Failed, OrchestrationRuntimeStatus.Failed)]
    [InlineData(OperationStatus.NotStarted, OrchestrationRuntimeStatus.Pending)]
    [InlineData(OperationStatus.Running, OrchestrationRuntimeStatus.Running, OrchestrationRuntimeStatus.ContinuedAsNew)]
    [InlineData(OperationStatus.Succeeded, OrchestrationRuntimeStatus.Completed)]
    public void GivenOperationStatus_WhenGettingRuntimeStatus_ThenMapCorrectly(OperationStatus status, params OrchestrationRuntimeStatus[] expected)
    {
        var query = new OperationQueryCondition<DicomOperation> { Statuses = new OperationStatus[] { status } };
        Assert.True(expected.SequenceEqual(query.ForDurableFunctions().RuntimeStatus));
    }
}
