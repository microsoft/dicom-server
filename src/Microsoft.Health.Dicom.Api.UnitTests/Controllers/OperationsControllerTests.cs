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
using Microsoft.Health.Dicom.Core.Features.Routing;
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
        public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OperationsController(
                null,
                Substitute.For<IUrlResolver>(),
                NullLogger<OperationsController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new OperationsController(
                new Mediator(t => null),
                null,
                NullLogger<OperationsController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new OperationsController(
                new Mediator(t => null),
                Substitute.For<IUrlResolver>(),
                null));
        }

        [Fact]
        public async Task GivenNullStatus_WhenGettingStatus_ThenReturnNotFound()
        {
            string id = Guid.NewGuid().ToString();
            IMediator mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var controller = new OperationsController(mediator, urlResolver, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns((OperationStatusResponse)null);

            Assert.IsType<NotFoundResult>(await controller.GetStatusAsync(id));
            Assert.False(controller.Response.Headers.ContainsKey(HeaderNames.Location));

            await mediator.Received(1).Send(
                Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                Arg.Is(controller.HttpContext.RequestAborted));
            urlResolver.DidNotReceiveWithAnyArgs().ResolveOperationStatusUri(default);
        }

        [Theory]
        [InlineData(OperationRuntimeStatus.Pending)]
        [InlineData(OperationRuntimeStatus.Running)]
        public async Task GivenInProgressStatus_WhenGettingStatus_ThenReturnOk(OperationRuntimeStatus inProgressStatus)
        {
            string id = Guid.NewGuid().ToString();
            string statusUrl = "https://dicom.contoso.io/unit/test/Operations/" + id;
            IMediator mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var controller = new OperationsController(mediator, urlResolver, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var expected = new OperationStatusResponse(
                id,
                OperationType.Reindex,
                DateTime.UtcNow.AddMinutes(-1),
                DateTime.UtcNow,
                inProgressStatus);

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(expected);
            urlResolver.ResolveOperationStatusUri(id).Returns(new Uri(statusUrl, UriKind.Absolute));

            IActionResult response = await controller.GetStatusAsync(id);
            Assert.IsType<ObjectResult>(response);
            Assert.True(controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues headerValue));
            Assert.Single(headerValue);

            var actual = response as ObjectResult;
            Assert.Equal((int)HttpStatusCode.Accepted, actual.StatusCode);
            Assert.Same(expected, actual.Value);
            Assert.Equal(statusUrl, headerValue[0]);

            await mediator.Received(1).Send(
                Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                Arg.Is(controller.HttpContext.RequestAborted));
            urlResolver.Received(1).ResolveOperationStatusUri(id);
        }

        [Theory]
        [InlineData(OperationRuntimeStatus.Unknown)]
        [InlineData(OperationRuntimeStatus.Completed)]
        [InlineData(OperationRuntimeStatus.Failed)]
        [InlineData(OperationRuntimeStatus.Canceled)]
        public async Task GivenDoneStatus_WhenGettingStatus_ThenReturnOk(OperationRuntimeStatus doneStatus)
        {
            string id = Guid.NewGuid().ToString();
            IMediator mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var controller = new OperationsController(mediator, urlResolver, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var expected = new OperationStatusResponse(
                id,
                OperationType.Reindex,
                DateTime.UtcNow,
                DateTime.UtcNow,
                doneStatus);

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(expected);

            IActionResult response = await controller.GetStatusAsync(id);
            Assert.IsType<ObjectResult>(response);
            Assert.False(controller.Response.Headers.ContainsKey(HeaderNames.Location));

            var actual = response as ObjectResult;
            Assert.Equal((int)HttpStatusCode.OK, actual.StatusCode);
            Assert.Same(expected, actual.Value);

            await mediator.Received(1).Send(
                Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                Arg.Is(controller.HttpContext.RequestAborted));
            urlResolver.DidNotReceiveWithAnyArgs().ResolveOperationStatusUri(default);
        }
    }
}
