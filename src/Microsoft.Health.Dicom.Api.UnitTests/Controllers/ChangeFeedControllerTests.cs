// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public class ChangeFeedControllerTests
{
    private readonly ChangeFeedController _controller;
    private readonly IApiVersioningFeature _apiVersion = Substitute.For<IApiVersioningFeature>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public ChangeFeedControllerTests()
    {
        _controller = new ChangeFeedController(_mediator, NullLogger<ChangeFeedController>.Instance);
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    [Theory]
    [InlineData(null, 10, ChangeFeedOrder.Sequence)]
    [InlineData(1, 10, ChangeFeedOrder.Sequence)]
    [InlineData(2, 100, ChangeFeedOrder.Timestamp)]
    [InlineData(3, 100, ChangeFeedOrder.Timestamp)]
    public async Task GivenApiVersion_WhenFetchingChangeFeed_ThenUseProperDefaults(int? version, int expectedDefaultLimit, ChangeFeedOrder order)
    {
        if (version.HasValue)
            _apiVersion.RequestedApiVersion.Returns(new ApiVersion(version.GetValueOrDefault(), 0));

        var expected = new List<ChangeFeedEntry>();

        _controller.HttpContext.Features.Set(_apiVersion);
        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == DateTimeOffsetRange.MaxValue &&
                    x.Offset == 0 &&
                    x.Limit == expectedDefaultLimit &&
                    x.IncludeMetadata &&
                    x.Order == order),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        IActionResult result = await _controller.GetChangeFeedAsync();
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == DateTimeOffsetRange.MaxValue &&
                    x.Offset == 0 &&
                    x.Limit == expectedDefaultLimit &&
                    x.IncludeMetadata &&
                    x.Order == order),
                _controller.HttpContext.RequestAborted);
    }

    [Theory]
    [InlineData(0, 5, null, null, false)]
    [InlineData(5, 25, "2023-04-26T14:50:55.0596678-07:00", null, true)]
    [InlineData(25, 1, null, "2023-04-26T14:50:55.0596678-07:00", false)]
    [InlineData(26, 3, "2023-04-26T14:50:55.0596678-07:00", "2023-04-26T14:54:18.6773316-07:00", true)]
    public async Task GivenChangeFeed_WhenFetchingLatest_ThenPassValues(int offset, int limit, string start, string end, bool includeMetadata)
    {
        DateTimeOffset? startTime = start != null ? DateTimeOffset.Parse(start, CultureInfo.InvariantCulture) : null;
        DateTimeOffset? endTime = end != null ? DateTimeOffset.Parse(end, CultureInfo.InvariantCulture) : null;
        var expectedRange = new DateTimeOffsetRange(startTime ?? DateTimeOffset.MinValue, endTime ?? DateTimeOffset.MaxValue);
        var expected = new List<ChangeFeedEntry>();

        _apiVersion.RequestedApiVersion.Returns(new ApiVersion(2, 0));
        _controller.HttpContext.Features.Set(_apiVersion);
        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == expectedRange &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata &&
                    x.Order == ChangeFeedOrder.Timestamp),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        IActionResult result = await _controller.GetChangeFeedAsync(offset, limit, startTime, endTime, includeMetadata);
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == expectedRange &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata &&
                    x.Order == ChangeFeedOrder.Timestamp),
                _controller.HttpContext.RequestAborted);
    }

    [Theory]
    [InlineData(null, 101)]
    [InlineData(1, 200)]
    [InlineData(2, 201)]
    [InlineData(3, 300)]
    public async Task GivenApiVersion_WhenFetchingChangeFeed_ThenUseProperMaxLimit(int? version, int limit)
    {
        if (version.HasValue)
            _apiVersion.RequestedApiVersion.Returns(new ApiVersion(version.GetValueOrDefault(), 0));

        _controller.HttpContext.Features.Set(_apiVersion);

        await Assert.ThrowsAsync<ChangeFeedLimitOutOfRangeException>(() => _controller.GetChangeFeedAsync(offset: 0, limit: limit));
    }
}
