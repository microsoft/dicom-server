// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Extensions;
using Microsoft.Health.Dicom.Operations.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Extensions;

public class DurableFunctionsContextExtensionsTests
{
    [Theory]
    [InlineData("foo", false)]
    [InlineData("5039637a547c4a0290d3633a57240dfd", true)]
    public void GivenInstanceId_WhenCheckingIfGuid_ThenReturnParseResult(string instanceId, bool isGuid)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Equal(isGuid, context.HasInstanceGuid());
    }

    [Fact]
    public void GivenInstanceId_WhenFetchingAsGuid_ThenReturnParsedValue()
    {
        Guid expected = Guid.NewGuid();
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(OperationId.ToString(expected));

        Assert.Equal(expected, context.GetInstanceGuid());
    }

    [Fact]
    public async Task GivenRunningOrchestration_WhenQueryingCreatedTime_ThenReturnCreatedTime()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        var expected = DateTime.UtcNow;

        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);

        var options = new RetryOptions(TimeSpan.FromSeconds(5), 3);

        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                options,
                Arg.Is<GetInstanceStatusInput>(x => x.InstanceId == operationId))
            .Returns(new DurableOrchestrationStatus { CreatedTime = expected });

        // Invoke
        DateTime actual = await context.GetCreatedTimeAsync(options);

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                options,
                Arg.Is<GetInstanceStatusInput>(x => x.InstanceId == operationId));
    }
}
