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
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Messages.WorkitemMessages;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers
{
    public sealed class WorkitemControllerAddTests
    {
        [Fact]
        public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WorkitemController(
                null,
                NullLogger<WorkitemController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new WorkitemController(
                new Mediator(t => null),
                null));
        }

        [Fact]
        public async Task GivenWorkitemInstanceUid_WhenHandlerFails_ThenReturnBadRequest()
        {
            var id = Guid.NewGuid();
            var mediator = Substitute.For<IMediator>();
            var controller = new WorkitemController(mediator, NullLogger<WorkitemController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            mediator
                .Send(
                    Arg.Is<AddWorkitemRequest>(x => x.WorkitemInstanceUid == id.ToString()),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(new AddWorkitemResponse(WorkitemResponseStatus.Failure, new Uri("https://www.microsoft.com")));

            ObjectResult result = await controller.AddAsync(id.ToString()) as ObjectResult;

            Assert.IsType<ObjectResult>(result);
            Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)result.StatusCode);
            Assert.False(controller.Response.Headers.ContainsKey(HeaderNames.ContentLocation));
        }

        [Fact]
        public async Task GivenWorkitemInstanceUid_WhenItAlreadyExists_ThenReturnConflict()
        {
            var id = Guid.NewGuid();
            var mediator = Substitute.For<IMediator>();
            var controller = new WorkitemController(mediator, NullLogger<WorkitemController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            mediator
                .Send(
                    Arg.Is<AddWorkitemRequest>(x => x.WorkitemInstanceUid == id.ToString()),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(new AddWorkitemResponse(WorkitemResponseStatus.Conflict, new Uri("https://www.microsoft.com")));

            ObjectResult result = await controller.AddAsync(id.ToString()) as ObjectResult;

            Assert.IsType<ObjectResult>(result);
            Assert.Equal(HttpStatusCode.Conflict, (HttpStatusCode)result.StatusCode);
            Assert.False(controller.Response.Headers.ContainsKey(HeaderNames.ContentLocation));
        }


        [Fact]
        public async Task GivenWorkitemInstanceUid_WhenHandlerSucceeds_ThenReturnCreated()
        {
            const string url = "https://www.git.com/";
            var id = Guid.NewGuid();
            var mediator = Substitute.For<IMediator>();
            var controller = new WorkitemController(mediator, NullLogger<WorkitemController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            mediator
                .Send(
                    Arg.Is<AddWorkitemRequest>(x => x.WorkitemInstanceUid == id.ToString()),
                    Arg.Is(controller.HttpContext.RequestAborted))
                .Returns(new AddWorkitemResponse(WorkitemResponseStatus.Success, new Uri(url)));

            ObjectResult result = await controller.AddAsync(id.ToString()) as ObjectResult;

            Assert.IsType<ObjectResult>(result);
            Assert.Equal(HttpStatusCode.Created, (HttpStatusCode)result.StatusCode);
            Assert.True(controller.Response.Headers.ContainsKey(HeaderNames.ContentLocation));
            Assert.Equal(url, controller.Response.Headers[HeaderNames.ContentLocation]);
            Assert.Equal(url, controller.Response.Headers[HeaderNames.Location]);
        }
    }
}
