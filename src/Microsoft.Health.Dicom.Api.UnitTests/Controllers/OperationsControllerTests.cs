// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers
{
    public class OperationsControllerTests
    {
        [Fact]
        public async Task GetOperationStatusAsync_GivenNullStatus_ReturnNotFound()
        {
            string id = Guid.NewGuid().ToString();
            IMediator mediator = Substitute.For<IMediator>();

            var controller = new OperationsController(mediator, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.Id == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns((OperationStatusResponse)null);

            Assert.IsType<NotFoundResult>(await controller.GetOperationStatusAsync(id));
            Assert.False(controller.Response.Headers.ContainsKey(HeaderNames.Location));

            await mediator.Received(1).Send(
                Arg.Is<OperationStatusRequest>(x => x.Id == id),
                Arg.Is(controller.HttpContext.RequestAborted));
        }

        [Theory]
        [InlineData(OperationStatus.Pending)]
        [InlineData(OperationStatus.Running)]
        public async Task GetOperationStatusAsync_GivenInProgressStatus_ReturnOk(OperationStatus inProgressStatus)
        {
            string id = Guid.NewGuid().ToString();
            IMediator mediator = Substitute.For<IMediator>();
            OperationStatusResponse expected = new OperationStatusResponse(
                id,
                OperationType.Reindex,
                DateTime.UtcNow,
                inProgressStatus);

            var controller = new OperationsController(mediator, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext.Request.PathBase = new PathString("/api/v1");
            controller.ControllerContext.HttpContext.Request.Path = new PathString("/Operations/" + id);

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.Id == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(expected);

            IActionResult response = await controller.GetOperationStatusAsync(id);
            Assert.IsType<ObjectResult>(response);
            Assert.True(controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues headerValue));
            Assert.Single(headerValue);

            var actual = response as ObjectResult;
            Assert.Equal((int)HttpStatusCode.Accepted, actual.StatusCode);
            Assert.Same(expected, actual.Value);
            Assert.Equal("/api/v1/Operations/" + id, headerValue[0]);

            await mediator.Received(1).Send(
                Arg.Is<OperationStatusRequest>(x => x.Id == id),
                Arg.Is(controller.HttpContext.RequestAborted));
        }

        [Theory]
        [InlineData(OperationStatus.Unknown)]
        [InlineData(OperationStatus.Completed)]
        [InlineData(OperationStatus.Failed)]
        [InlineData(OperationStatus.Canceled)]
        public async Task GetOperationStatusAsync_GivenDoneStatus_ReturnOk(OperationStatus doneStatus)
        {
            string id = Guid.NewGuid().ToString();
            IMediator mediator = Substitute.For<IMediator>();
            OperationStatusResponse expected = new OperationStatusResponse(
                id,
                OperationType.Reindex,
                DateTime.UtcNow,
                doneStatus);

            var controller = new OperationsController(mediator, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext.Request.PathBase = new PathString("/api/v1");
            controller.ControllerContext.HttpContext.Request.Path = new PathString("/Operations/" + id);

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.Id == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(expected);

            IActionResult response = await controller.GetOperationStatusAsync(id);
            Assert.IsType<ObjectResult>(response);
            Assert.False(controller.Response.Headers.ContainsKey(HeaderNames.Location));

            var actual = response as ObjectResult;
            Assert.Equal((int)HttpStatusCode.OK, actual.StatusCode);
            Assert.Same(expected, actual.Value);

            await mediator.Received(1).Send(
                Arg.Is<OperationStatusRequest>(x => x.Id == id),
                Arg.Is(controller.HttpContext.RequestAborted));
        }
    }
}
