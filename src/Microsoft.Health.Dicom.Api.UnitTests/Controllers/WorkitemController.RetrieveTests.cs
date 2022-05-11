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
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public sealed class WorkitemControllerRetrieveTests
{
    private readonly WorkitemController _controller;
    private readonly IMediator _mediator;
    private readonly string _id = Guid.NewGuid().ToString();

    public WorkitemControllerRetrieveTests()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new WorkitemController(_mediator, NullLogger<WorkitemController>.Instance);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.ControllerContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { { _id, StringValues.Empty } });
    }

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
        _mediator
            .Send(
                Arg.Is<RetrieveWorkitemRequest>(x => x.WorkitemInstanceUid == _id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new RetrieveWorkitemResponse(WorkitemResponseStatus.Failure, new DicomDataset(), string.Empty));

        var result = await _controller.RetrieveAsync(_id) as ObjectResult;

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)result.StatusCode);
    }

    [Fact]
    public async Task GivenWorkitemInstanceUid_WhenHandlerSucceeds_ThenReturnOK()
    {
        _mediator
            .Send(
                Arg.Is<RetrieveWorkitemRequest>(x => x.WorkitemInstanceUid == _id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new RetrieveWorkitemResponse(WorkitemResponseStatus.Success, new DicomDataset(), string.Empty));

        var result = await _controller.RetrieveAsync(_id) as ObjectResult;

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)result.StatusCode);
    }

    [Fact]
    public async Task GivenWorkitemInstanceUid_WhenHandlerFails_ThenReturnNotFound()
    {
        _mediator
            .Send(
                Arg.Is<RetrieveWorkitemRequest>(x => x.WorkitemInstanceUid == _id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new RetrieveWorkitemResponse(WorkitemResponseStatus.NotFound, new DicomDataset(), string.Empty));

        var result = await _controller.RetrieveAsync(_id) as ObjectResult;

        Assert.IsType<ObjectResult>(result);
        Assert.Equal(HttpStatusCode.NotFound, (HttpStatusCode)result.StatusCode);
    }

}
