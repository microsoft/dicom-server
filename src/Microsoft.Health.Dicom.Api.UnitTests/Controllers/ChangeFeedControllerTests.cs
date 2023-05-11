// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public class ChangeFeedControllerTests
{
    private readonly ChangeFeedController _controller;
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public ChangeFeedControllerTests()
    {
        _controller = new ChangeFeedController(_mediator, NullLogger<ChangeFeedController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task GivenNoUserValues_WhenFetchingChangeFeed_ThenUseProperDefaults()
    {
        var expected = new List<ChangeFeedEntry>();

        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == TimeRange.MaxValue &&
                    x.Offset == 0L &&
                    x.Limit == 10 &&
                    x.IncludeMetadata),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        IActionResult result = await _controller.GetChangeFeedAsync();
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == TimeRange.MaxValue &&
                    x.Offset == 0L &&
                    x.Limit == 10 &&
                    x.IncludeMetadata),
                _controller.HttpContext.RequestAborted);
    }

    [Theory]
    [InlineData(0, 5, null, null, false)]
    [InlineData(int.MaxValue + 1L, 25, "2023-05-10T01:23:45Z", null, true)]
    [InlineData(100, 200, null, "2023-05-11T01:23:45Z", true)]
    [InlineData(1000, int.MaxValue, "2023-05-10T01:23:45Z", "2023-05-11T01:23:45Z", false)]
    public async Task GivenParameters_WhenFetchingChangeFeed_ThenPassValues(long offset, int limit, string start, string end, bool includeMetadata)
    {
        DateTimeOffset? startTime = start == null ? null : DateTimeOffset.Parse(start, CultureInfo.InvariantCulture);
        DateTimeOffset? endTime = end == null ? null : DateTimeOffset.Parse(end, CultureInfo.InvariantCulture);
        var range = new TimeRange(startTime ?? DateTimeOffset.MinValue, endTime ?? DateTimeOffset.MaxValue);

        var expected = new List<ChangeFeedEntry>();

        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == range &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        IActionResult result = await _controller.GetChangeFeedAsync(offset, limit, startTime, endTime, includeMetadata);
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == range &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata),
                _controller.HttpContext.RequestAborted);
    }

    [Fact]
    public async Task GivenOrderMixup_WhenFetchingChangeFeed_ThenThrow()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _controller.GetChangeFeedAsync(startTime: DateTimeOffset.UtcNow, endTime: DateTimeOffset.UtcNow.AddHours(-1)));
        await _mediator.DidNotReceiveWithAnyArgs().Send(default, default);
    }

    [Fact]
    public async Task GivenNoUserValues_WhenFetchingWindowedChangeFeed_ThenUseProperDefaults()
    {
        var expected = new List<ChangeFeedEntry>();

        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == TimeRange.MaxValue &&
                    x.Offset == 0L &&
                    x.Limit == 100 &&
                    x.IncludeMetadata),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        IActionResult result = await _controller.GetChangeFeedAsync(new WindowedPaginationOptions());
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == TimeRange.MaxValue &&
                    x.Offset == 0L &&
                    x.Limit == 100 &&
                    x.IncludeMetadata),
                _controller.HttpContext.RequestAborted);
    }

    [Theory]
    [InlineData(0, 5, null, null, false)]
    [InlineData(5, 25, "2023-04-26T14:50:55.0596678-07:00", null, true)]
    [InlineData(25, 1, null, "2023-04-26T14:50:55.0596678-07:00", false)]
    [InlineData(int.MaxValue + 10L, 3, "2023-04-26T14:50:55.0596678-07:00", "2023-04-26T14:54:18.6773316-07:00", true)]
    public async Task GivenParameters_WhenFetchingWindowedChangeFeed_ThenPassValues(long offset, int limit, string start, string end, bool includeMetadata)
    {
        DateTimeOffset? startTime = start != null ? DateTimeOffset.Parse(start, CultureInfo.InvariantCulture) : null;
        DateTimeOffset? endTime = end != null ? DateTimeOffset.Parse(end, CultureInfo.InvariantCulture) : null;
        var expectedRange = new TimeRange(startTime ?? DateTimeOffset.MinValue, endTime ?? DateTimeOffset.MaxValue);
        var expected = new List<ChangeFeedEntry>();

        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == expectedRange &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        var options = new WindowedPaginationOptions
        {
            EndTime = endTime,
            Limit = limit,
            Offset = offset,
            StartTime = startTime,
        };

        IActionResult result = await _controller.GetChangeFeedAsync(options, includeMetadata);
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == expectedRange &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata),
                _controller.HttpContext.RequestAborted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GivenParameters_WhenFetchingLatestChangeFeed_ThenPassValues(bool includeMetadata)
    {
        var expected = new ChangeFeedEntry(1, DateTimeOffset.UtcNow, ChangeFeedAction.Create, "1", "2", "3", 1, 1, ChangeFeedState.Current);

        _mediator
            .Send(
                Arg.Is<ChangeFeedLatestRequest>(x => x.IncludeMetadata == includeMetadata),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedLatestResponse(expected));

        IActionResult result = await _controller.GetChangeFeedLatestAsync(includeMetadata);
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedLatestRequest>(x => x.IncludeMetadata == includeMetadata),
                _controller.HttpContext.RequestAborted);
    }
}
