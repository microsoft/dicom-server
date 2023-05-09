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
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Operations;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Health.Operations;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public class OperationsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IUrlResolver _urlResolver = Substitute.For<IUrlResolver>();
    private readonly IApiVersioningFeature _apiVersion = Substitute.For<IApiVersioningFeature>();
    private readonly OperationsController _controller;

    public OperationsControllerTests()
    {
        _apiVersion.RequestedApiVersion.Returns(new ApiVersion(1, 0));

        _controller = new OperationsController(_mediator, _urlResolver, NullLogger<OperationsController>.Instance);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.HttpContext.Features.Set(_apiVersion);
    }

    [Fact]
    public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
    {
        var mediator = new Mediator(null);

        Assert.Throws<ArgumentNullException>(() => new OperationsController(
            null,
            Substitute.For<IUrlResolver>(),
            NullLogger<OperationsController>.Instance));

        Assert.Throws<ArgumentNullException>(() => new OperationsController(
            mediator,
            null,
            NullLogger<OperationsController>.Instance));

        Assert.Throws<ArgumentNullException>(() => new OperationsController(
            mediator,
            Substitute.For<IUrlResolver>(),
            null));
    }

    [Fact]
    public async Task GivenNullState_WhenGettingState_ThenReturnNotFound()
    {
        Guid id = Guid.NewGuid();

        _mediator
            .Send(
                Arg.Is<OperationStateRequest>(x => x.OperationId == id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns((OperationStateResponse)null);

        Assert.IsType<NotFoundResult>(await _controller.GetStateAsync(id));
        Assert.False(_controller.Response.Headers.ContainsKey(HeaderNames.Location));

        await _mediator.Received(1).Send(
            Arg.Is<OperationStateRequest>(x => x.OperationId == id),
            Arg.Is(_controller.HttpContext.RequestAborted));
        _urlResolver.DidNotReceiveWithAnyArgs().ResolveOperationStatusUri(default);
    }

    [Theory]
    [InlineData(OperationStatus.NotStarted)]
    [InlineData(OperationStatus.Running)]
    public async Task GivenInProgressState_WhenGettingState_ThenReturnOk(OperationStatus inProgressStatus)
    {
        Guid id = Guid.NewGuid();
        string statusUrl = "https://dicom.contoso.io/unit/test/Operations/" + id;

        var expected = new OperationState<DicomOperation>
        {
            CreatedTime = DateTime.UtcNow.AddMinutes(-1),
            LastUpdatedTime = DateTime.UtcNow,
            OperationId = id,
            PercentComplete = inProgressStatus == OperationStatus.NotStarted ? 0 : 37,
            Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
            Status = inProgressStatus,
            Type = DicomOperation.Reindex,
        };

        _mediator
            .Send(
                Arg.Is<OperationStateRequest>(x => x.OperationId == id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new OperationStateResponse(expected));
        _urlResolver.ResolveOperationStatusUri(id).Returns(new Uri(statusUrl, UriKind.Absolute));

        IActionResult response = await _controller.GetStateAsync(id);
        Assert.IsType<ObjectResult>(response);
        Assert.True(_controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues headerValue));
        Assert.Single(headerValue);

        var actual = response as ObjectResult;
        Assert.Equal((int)HttpStatusCode.Accepted, actual.StatusCode);
        Assert.Same(expected, actual.Value);
        Assert.Equal(statusUrl, headerValue[0]);

        await _mediator.Received(1).Send(
            Arg.Is<OperationStateRequest>(x => x.OperationId == id),
            Arg.Is(_controller.HttpContext.RequestAborted));
        _urlResolver.Received(1).ResolveOperationStatusUri(id);
    }

    [Theory]
    [InlineData(OperationStatus.Unknown)]
    [InlineData(OperationStatus.Failed)]
    [InlineData(OperationStatus.Canceled)]
    public async Task GivenDoneState_WhenGettingState_ThenReturnOk(OperationStatus doneStatus)
    {
        Guid id = Guid.NewGuid();

        var expected = new OperationState<DicomOperation>
        {
            CreatedTime = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedTime = DateTime.UtcNow,
            OperationId = id,
            PercentComplete = doneStatus == OperationStatus.Succeeded ? 100 : 71,
            Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
            Status = doneStatus,
            Type = DicomOperation.Reindex,
        };

        _mediator
            .Send(
                Arg.Is<OperationStateRequest>(x => x.OperationId == id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new OperationStateResponse(expected));

        IActionResult response = await _controller.GetStateAsync(id);
        Assert.IsType<ObjectResult>(response);
        Assert.False(_controller.Response.Headers.ContainsKey(HeaderNames.Location));

        var actual = response as ObjectResult;
        Assert.Equal((int)HttpStatusCode.OK, actual.StatusCode);
        Assert.Same(expected, actual.Value);

        await _mediator.Received(1).Send(
            Arg.Is<OperationStateRequest>(x => x.OperationId == id),
            Arg.Is(_controller.HttpContext.RequestAborted));
        _urlResolver.DidNotReceiveWithAnyArgs().ResolveOperationStatusUri(default);
    }

    [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
    [InlineData(null, OperationStatus.Completed)]
    [InlineData(1, OperationStatus.Completed)]
#pragma warning restore CS0618 // Type or member is obsolete
    [InlineData(2, OperationStatus.Succeeded)]
    [InlineData(3, OperationStatus.Succeeded)]
    public async Task GivenSucceededState_WhenGettingState_ThenReturnProperDoneStatus(int? apiVersion, OperationStatus expectedStatus)
    {
        Guid id = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;

        _apiVersion.RequestedApiVersion.Returns(apiVersion.HasValue ? new ApiVersion(apiVersion.GetValueOrDefault(), 0) : null);

        var expected = new OperationState<DicomOperation, object>
        {
            CreatedTime = utcNow.AddMinutes(-5),
            LastUpdatedTime = utcNow,
            OperationId = id,
            PercentComplete = 100,
            Resources = new Uri[] { new Uri("https://dicom.contoso.io/unit/test/extendedquerytags/00101010", UriKind.Absolute) },
            Results = new object(),
            Status = expectedStatus,
            Type = DicomOperation.Reindex,
        };

        _mediator
            .Send(
                Arg.Is<OperationStateRequest>(x => x.OperationId == id),
                Arg.Is(_controller.HttpContext.RequestAborted))
            .Returns(new OperationStateResponse(expected));

        IActionResult response = await _controller.GetStateAsync(id);
        Assert.IsType<ObjectResult>(response);
        Assert.False(_controller.Response.Headers.ContainsKey(HeaderNames.Location));

        var objectResult = response as ObjectResult;
        Assert.Equal((int)HttpStatusCode.OK, objectResult.StatusCode);

        var actual = objectResult.Value as IOperationState<DicomOperation>;
        Assert.Equal(expected.CreatedTime, actual.CreatedTime);
        Assert.Equal(expected.LastUpdatedTime, actual.LastUpdatedTime);
        Assert.Equal(expected.OperationId, actual.OperationId);
        Assert.Equal(expected.PercentComplete, actual.PercentComplete);
        Assert.Same(expected.Resources, actual.Resources);
        Assert.Same(expected.Results, actual.Results);
        Assert.Equal(expectedStatus, actual.Status);
        Assert.Equal(expected.Type, actual.Type);

        await _mediator.Received(1).Send(
            Arg.Is<OperationStateRequest>(x => x.OperationId == id),
            Arg.Is(_controller.HttpContext.RequestAborted));
        _urlResolver.DidNotReceiveWithAnyArgs().ResolveOperationStatusUri(default);
    }
}
