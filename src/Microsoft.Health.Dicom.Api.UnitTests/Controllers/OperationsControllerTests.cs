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
            Guid id = Guid.NewGuid();
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
        [InlineData(OperationRuntimeStatus.NotStarted)]
        [InlineData(OperationRuntimeStatus.Running)]
        public async Task GivenInProgressStatus_WhenGettingStatus_ThenReturnOk(OperationRuntimeStatus inProgressStatus)
        {
            Guid id = Guid.NewGuid();
            string statusUrl = "https://dicom.contoso.io/unit/test/Operations/" + id;
            IMediator mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var controller = new OperationsController(mediator, urlResolver, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var expected = new OperationStatus
            {
                CreatedTime = DateTime.UtcNow.AddMinutes(-1),
                LastUpdatedTime = DateTime.UtcNow,
                OperationId = id,
                PercentComplete = inProgressStatus == OperationRuntimeStatus.NotStarted ? 0 : 37,
                Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
                Status = inProgressStatus,
                Type = OperationType.Reindex,
            };

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(new OperationStatusResponse(expected));
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
            Guid id = Guid.NewGuid();
            IMediator mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var controller = new OperationsController(mediator, urlResolver, NullLogger<OperationsController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var expected = new OperationStatus
            {
                CreatedTime = DateTime.UtcNow.AddMinutes(-5),
                LastUpdatedTime = DateTime.UtcNow,
                OperationId = id,
                PercentComplete = doneStatus == OperationRuntimeStatus.Completed ? 100 : 71,
                Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
                Status = doneStatus,
                Type = OperationType.Reindex,
            };

            mediator
                .Send(
                    Arg.Is<OperationStatusRequest>(x => x.OperationId == id),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(new OperationStatusResponse(expected));

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
