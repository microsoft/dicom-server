// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public sealed class WorkitemControllerAddTests
{
    private readonly WorkitemController _controller;
    private readonly IMediator _mediator;
    private readonly string _id = Guid.NewGuid().ToString();

    public WorkitemControllerAddTests()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new WorkitemController(_mediator, NullLogger<WorkitemController>.Instance);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { _id, StringValues.Empty } });

    }

    [Fact]
    public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
    {
        var mediator = new Mediator(null);
        Assert.Throws<ArgumentNullException>(() => new WorkitemController(
            null,
            NullLogger<WorkitemController>.Instance));

        Assert.Throws<ArgumentNullException>(() => new WorkitemController(
            mediator,
            null));
    }

    [Fact]
    public async Task GivenWorkitemInstanceUid_WhenHandlerFails_ThenReturnBadRequest()
    {
        _mediator
            .Send(
                Arg.Is<AddWorkitemRequest>(x => x.WorkitemInstanceUid == _id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new AddWorkitemResponse(WorkitemResponseStatus.Failure, new Uri("https://www.microsoft.com")));

        ObjectResult result = await _controller.AddAsync(new DicomDataset[1]) as ObjectResult;

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)result.StatusCode);
        Assert.False(_controller.Response.Headers.ContainsKey(HeaderNames.ContentLocation));
    }

    [Fact]
    public async Task GivenWorkitemInstanceUid_WhenItAlreadyExists_ThenReturnConflict()
    {
        _mediator
            .Send(
                Arg.Is<AddWorkitemRequest>(x => x.WorkitemInstanceUid == _id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new AddWorkitemResponse(WorkitemResponseStatus.Conflict, new Uri("https://www.microsoft.com")));

        ObjectResult result = await _controller.AddAsync(new DicomDataset[1]) as ObjectResult;

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(HttpStatusCode.Conflict, (HttpStatusCode)result.StatusCode);
        Assert.False(_controller.Response.Headers.ContainsKey(HeaderNames.ContentLocation));
    }


    [Fact]
    public async Task GivenWorkitemInstanceUid_WhenHandlerSucceeds_ThenReturnCreated()
    {
        var url = "https://www.microsoft.com/";
        _mediator
            .Send(
                Arg.Is<AddWorkitemRequest>(x => x.WorkitemInstanceUid == _id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new AddWorkitemResponse(WorkitemResponseStatus.Success, new Uri(url)));

        ObjectResult result = await _controller.AddAsync(new DicomDataset[1]) as ObjectResult;

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(HttpStatusCode.Created, (HttpStatusCode)result.StatusCode);
        Assert.True(_controller.Response.Headers.ContainsKey(HeaderNames.ContentLocation));
        Assert.Equal(url, _controller.Response.Headers[HeaderNames.ContentLocation]);
        Assert.Equal(url, _controller.Response.Headers[HeaderNames.Location]);
    }
}
