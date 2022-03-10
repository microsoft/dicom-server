// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Management
{
    public class DurableOrchestrationClientActivityTests
    {
        [Fact]
        public async Task GivenNoInstance_WhenQueryingStatus_ThenReturnNull()
        {
            // Arrange input
            string instanceId = OperationId.Generate();

            IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
            context.GetInput<GetInstanceStatusInput>().Returns(
                new GetInstanceStatusInput
                {
                    InstanceId = instanceId,
                    ShowHistory = false,
                    ShowHistoryOutput = false,
                    ShowInput = false,
                });

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client.GetStatusAsync(instanceId, false, false, false).Returns(Task.FromResult<DurableOrchestrationStatus>(null));

            // Call activity
            DurableOrchestrationStatus actual = await DurableOrchestrationClientActivity.GetInstanceStatusAsync(context, client, NullLogger.Instance);

            // Assert behavior
            Assert.Null(actual);
            context.Received(1).GetInput<GetInstanceStatusInput>();
            await client.Received(1).GetStatusAsync(instanceId, false, false, false);
        }

        [Fact]
        public async Task GivenValidInstance_WhenQueryingStatus_ThenReturnStatus()
        {
            // Arrange input
            string instanceId = OperationId.Generate();
            var expected = new DurableOrchestrationStatus { InstanceId = instanceId };

            IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
            context.GetInput<GetInstanceStatusInput>().Returns(
                new GetInstanceStatusInput
                {
                    InstanceId = instanceId,
                    ShowHistory = true,
                    ShowHistoryOutput = true,
                    ShowInput = false,
                });

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client.GetStatusAsync(instanceId, true, true, false).Returns(Task.FromResult(expected));

            // Call activity
            DurableOrchestrationStatus actual = await DurableOrchestrationClientActivity.GetInstanceStatusAsync(context, client, NullLogger.Instance);

            // Assert behavior
            Assert.Same(expected, actual);
            context.Received(1).GetInput<GetInstanceStatusInput>();
            await client.Received(1).GetStatusAsync(instanceId, true, true, false);
        }
    }
}
