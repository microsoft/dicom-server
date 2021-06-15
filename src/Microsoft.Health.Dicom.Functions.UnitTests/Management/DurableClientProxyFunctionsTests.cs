// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Management
{
    public class DurableClientProxyFunctionsTests
    {
        [Fact]
        public async Task GetStatusAsync_GivenNullArguments_ThrowsException()
        {
            var context = new DefaultHttpContext();
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            string id = Guid.NewGuid().ToString();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => DurableClientProxyFunctions.GetStatusAsync(null, client, id, NullLogger.Instance));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => DurableClientProxyFunctions.GetStatusAsync(context.Request, null, id, NullLogger.Instance));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => DurableClientProxyFunctions.GetStatusAsync(context.Request, client, id, null));

            await client.DidNotReceiveWithAnyArgs().GetStatusAsync(default(string));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t  \r\n")]
        public async Task GetStatusAsync_GivenInvalidId_ReturnsBadRequest(string id)
        {
            var context = new DefaultHttpContext();
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            Assert.IsType<BadRequestResult>(
                await DurableClientProxyFunctions.GetStatusAsync(context.Request, client, id, NullLogger.Instance));

            await client.DidNotReceiveWithAnyArgs().GetStatusAsync(default(string));
        }

        [Fact]
        public async Task GetStatusAsync_GivenNullStatus_ReturnsNotFound()
        {
            var context = new DefaultHttpContext();
            string id = Guid.NewGuid().ToString();

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client.GetStatusAsync(id, showHistory: false, showHistoryOutput: false, showInput: false)
                .Returns((DurableOrchestrationStatus)null);

            Assert.IsType<NotFoundResult>(await DurableClientProxyFunctions.GetStatusAsync(
                context.Request,
                client,
                id,
                NullLogger.Instance));

            await client.Received(1).GetStatusAsync(id, showHistory: false, showHistoryOutput: false, showInput: false);
        }

        [Fact]
        public async Task GetStatusAsync_GivenNonNullStatus_ReturnsOk()
        {
            var context = new DefaultHttpContext();
            string id = Guid.NewGuid().ToString();
            var status = new DurableOrchestrationStatus
            {
                CreatedTime = DateTime.UtcNow.AddMinutes(-2),
                LastUpdatedTime = DateTime.UtcNow,
                Name = "Example",
                RuntimeStatus = OrchestrationRuntimeStatus.Running,
            };

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client.GetStatusAsync(id, showHistory: false, showHistoryOutput: false, showInput: false).Returns(status);

            var result = await DurableClientProxyFunctions.GetStatusAsync(
                context.Request,
                client,
                id,
                NullLogger.Instance) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Same(status, result.Value);

            await client.Received(1).GetStatusAsync(id, showHistory: false, showHistoryOutput: false, showInput: false);
        }
    }
}
