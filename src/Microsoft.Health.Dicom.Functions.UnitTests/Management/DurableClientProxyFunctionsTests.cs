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
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Management
{
    public class DurableClientProxyFunctionsTests
    {
        [Fact]
        public async Task GivenNullArguments_WhenGettingStatus_ThenThrowException()
        {
            var context = new DefaultHttpContext();
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            Guid id = Guid.NewGuid();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => DurableClientProxyFunctions.GetStatusAsync(null, client, id, NullLogger.Instance));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => DurableClientProxyFunctions.GetStatusAsync(context.Request, null, id, NullLogger.Instance));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => DurableClientProxyFunctions.GetStatusAsync(context.Request, client, id, null));

            await client.DidNotReceiveWithAnyArgs().GetStatusAsync(default(string));
        }

        [Fact]
        public async Task GivenInvalidId_WhenGettingStatus_ThenReturnBadRequest()
        {
            var context = new DefaultHttpContext();
            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();

            Assert.IsType<NotFoundResult>(
                await DurableClientProxyFunctions.GetStatusAsync(context.Request, client, Guid.Empty, NullLogger.Instance));

            await client.DidNotReceiveWithAnyArgs().GetStatusAsync(default(string));
        }

        [Fact]
        public async Task GivenNullStatus_WhenGettingStatus_ThenReturnNotFound()
        {
            var context = new DefaultHttpContext();
            Guid id = Guid.NewGuid();

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client
                .GetStatusAsync(OperationId.ToString(id), showHistory: false, showHistoryOutput: false, showInput: false)
                .Returns((DurableOrchestrationStatus)null);

            Assert.IsType<NotFoundResult>(await DurableClientProxyFunctions.GetStatusAsync(
                context.Request,
                client,
                id,
                NullLogger.Instance));

            await client
                .Received(1)
                .GetStatusAsync(OperationId.ToString(id), showHistory: false, showHistoryOutput: false, showInput: false);
        }

        [Fact]
        public async Task GivenInvalidName_WhenGettingStatus_ThenReturnNotFound()
        {
            var context = new DefaultHttpContext();
            Guid id = Guid.NewGuid();
            var status = new DurableOrchestrationStatus
            {
                InstanceId = OperationId.ToString(id),
                CreatedTime = DateTime.UtcNow.AddMinutes(-2),
                LastUpdatedTime = DateTime.UtcNow,
                Name = "Foo",
                RuntimeStatus = OrchestrationRuntimeStatus.Running,
            };

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client
                .GetStatusAsync(OperationId.ToString(id), showHistory: false, showHistoryOutput: false, showInput: false)
                .Returns(status);

            Assert.IsType<NotFoundResult>(await DurableClientProxyFunctions.GetStatusAsync(
                context.Request,
                client,
                id,
                NullLogger.Instance));

            await client
                .Received(1)
                .GetStatusAsync(OperationId.ToString(id), showHistory: false, showHistoryOutput: false, showInput: false);
        }

        [Fact]
        public async Task GivenValidStatus_WhenGettingStatus_ThenReturnOk()
        {
            var context = new DefaultHttpContext();
            Guid id = Guid.NewGuid();
            var status = new DurableOrchestrationStatus
            {
                InstanceId = OperationId.ToString(id),
                CreatedTime = DateTime.UtcNow.AddMinutes(-2),
                LastUpdatedTime = DateTime.UtcNow,
                Name = nameof(ReindexDurableFunction.ReindexInstancesAsync),
                RuntimeStatus = OrchestrationRuntimeStatus.Running,
            };

            IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
            client
                .GetStatusAsync(OperationId.ToString(id), showHistory: false, showHistoryOutput: false, showInput: false)
                .Returns(status);

            var result = await DurableClientProxyFunctions.GetStatusAsync(
                context.Request,
                client,
                id,
                NullLogger.Instance) as OkObjectResult;

            var actual = result?.Value as OperationStatusResponse;
            Assert.NotNull(actual);
            Assert.Equal(id, actual.OperationId);
            Assert.Equal(OperationType.Reindex, actual.Type);
            Assert.Equal(status.CreatedTime, actual.CreatedTime);
            Assert.Equal(status.LastUpdatedTime, actual.LastUpdatedTime);
            Assert.Equal(OperationRuntimeStatus.Running, actual.Status);

            await client
                .Received(1)
                .GetStatusAsync(OperationId.ToString(id), showHistory: false, showHistoryOutput: false, showInput: false);
        }
    }
}
