// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Operations.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Operations.UnitTests.Extensions
{
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
    }
}
