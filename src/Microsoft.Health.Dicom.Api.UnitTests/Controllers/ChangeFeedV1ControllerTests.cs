// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Messages.ChangeFeed;
using Microsoft.Health.Dicom.Core.Models;
using Microsoft.Health.Dicom.Core.Models.ChangeFeed;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Controllers;

public class ChangeFeedV1ControllerTests
{
    private readonly ChangeFeedV1Controller _controller;
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public ChangeFeedV1ControllerTests()
    {
        _controller = new ChangeFeedV1Controller(_mediator, NullLogger<ChangeFeedV1Controller>.Instance);
    }

    [Fact]
    public async Task GivenNoUserValues_WhenFetchingChangeFeed_ThenUseProperDefaults()
    {
        var expected = new List<ChangeFeedEntry>();

        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == DateTimeOffsetRange.MaxValue &&
                    x.Offset == 0L &&
                    x.Limit == 10 &&
                    x.IncludeMetadata &&
                    x.Order == ChangeFeedOrder.Sequence),
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
                    x.Offset == 0L &&
                    x.Limit == 10 &&
                    x.IncludeMetadata &&
                    x.Order == ChangeFeedOrder.Sequence),
                _controller.HttpContext.RequestAborted);
    }

    [Theory]
    [InlineData(0, 5, false)]
    [InlineData(int.MaxValue + 1L, 25, true)]
    public async Task GivenParameters_WhenFetchingChangeFeed_ThenPassValues(long offset, int limit, bool includeMetadata)
    {
        var expected = new List<ChangeFeedEntry>();

        _mediator
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == DateTimeOffsetRange.MaxValue &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata &&
                    x.Order == ChangeFeedOrder.Sequence),
                _controller.HttpContext.RequestAborted)
            .Returns(new ChangeFeedResponse(expected));

        IActionResult result = await _controller.GetChangeFeedAsync(offset, limit, includeMetadata);
        var actual = result as ObjectResult;
        Assert.Same(expected, actual.Value);

        await _mediator
            .Received(1)
            .Send(
                Arg.Is<ChangeFeedRequest>(x =>
                    x.Range == DateTimeOffsetRange.MaxValue &&
                    x.Offset == offset &&
                    x.Limit == limit &&
                    x.IncludeMetadata == includeMetadata &&
                    x.Order == ChangeFeedOrder.Sequence),
                _controller.HttpContext.RequestAborted);
    }
}
