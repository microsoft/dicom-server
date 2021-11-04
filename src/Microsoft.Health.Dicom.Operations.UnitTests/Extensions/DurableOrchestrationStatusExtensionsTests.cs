// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Extensions;
using Microsoft.Health.Dicom.Operations.Indexing;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Extensions
{
    public class DurableOrchestrationStatusExtensionsTests
    {
        [Theory]
        [InlineData(null, OperationType.Unknown)]
        [InlineData("foo", OperationType.Unknown)]
        [InlineData("Unknown", OperationType.Unknown)]
        [InlineData(nameof(ReindexDurableFunction.ReindexInstancesAsync), OperationType.Reindex)]
        [InlineData("reindexINSTANCESasync", OperationType.Reindex)]
        public void GivenOrchestrationStatus_WhenGettingOperationType_ThenConvertNameToType(string name, OperationType expected)
        {
            Assert.Equal(expected, new DurableOrchestrationStatus { Name = name }.GetOperationType());
        }

        [Theory]
        [InlineData((OrchestrationRuntimeStatus)47, OperationRuntimeStatus.Unknown)]
        [InlineData(OrchestrationRuntimeStatus.Unknown, OperationRuntimeStatus.Unknown)]
        [InlineData(OrchestrationRuntimeStatus.Pending, OperationRuntimeStatus.NotStarted)]
        [InlineData(OrchestrationRuntimeStatus.Running, OperationRuntimeStatus.Running)]
        [InlineData(OrchestrationRuntimeStatus.ContinuedAsNew, OperationRuntimeStatus.Running)]
        [InlineData(OrchestrationRuntimeStatus.Completed, OperationRuntimeStatus.Completed)]
        [InlineData(OrchestrationRuntimeStatus.Failed, OperationRuntimeStatus.Failed)]
        [InlineData(OrchestrationRuntimeStatus.Canceled, OperationRuntimeStatus.Canceled)]
        [InlineData(OrchestrationRuntimeStatus.Terminated, OperationRuntimeStatus.Canceled)]
        public void GivenOrchestrationStatus_WhenGettingOperationRuntimeStatus_ThenConvertStatus(
            OrchestrationRuntimeStatus actual, OperationRuntimeStatus expected)
        {
            Assert.Equal(expected, new DurableOrchestrationStatus { RuntimeStatus = actual }.GetOperationRuntimeStatus());
        }
    }
}
