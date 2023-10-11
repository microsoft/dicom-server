// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using FellowOakDicom;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Api.Models;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Operations;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions;

public class ExtendedQueryTagControllerTests
{
    private readonly IOptions<FeatureConfiguration> _featureConfiguration;

    public ExtendedQueryTagControllerTests()
    {
        _featureConfiguration = Options.Create(new FeatureConfiguration { DisableOperations = false });
    }

    [Fact]
    public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
    {
        var mediator = new Mediator(null);
        Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(null, NullLogger<ExtendedQueryTagController>.Instance, _featureConfiguration));
        Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(mediator, null, _featureConfiguration));
    }

    [Fact]
    public async Task GivenExistingReindex_WhenCallingApi_ThenShouldReturnConflict()
    {
        IMediator mediator = Substitute.For<IMediator>();
        const string path = "11330001";

        var controller = new ExtendedQueryTagController(mediator, NullLogger<ExtendedQueryTagController>.Instance, _featureConfiguration);
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        Guid id = Guid.NewGuid();
        var expected = new OperationReference(id, new Uri("https://dicom.contoso.io/unit/test/Operations/" + id, UriKind.Absolute));

        mediator
            .Send(
                Arg.Is<AddExtendedQueryTagRequest>(x => x.ExtendedQueryTags.Single().Path == path),
                controller.HttpContext.RequestAborted)
            .Returns(Task.FromException<AddExtendedQueryTagResponse>(new ExistingOperationException(expected, "re-index")));

        var actual = await controller.PostAsync(new AddExtendedQueryTagEntry[] { new AddExtendedQueryTagEntry { Path = path } }) as ContentResult;
        await mediator.Received(1).Send(
            Arg.Is<AddExtendedQueryTagRequest>(x => x.ExtendedQueryTags.Single().Path == path),
            controller.HttpContext.RequestAborted);

        Assert.NotNull(actual);
        Assert.True(controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues header));
        Assert.Single(header);

        Assert.Equal((int)HttpStatusCode.Conflict, actual.StatusCode);
        Assert.Equal(MediaTypeNames.Text.Plain, actual.ContentType);
        Assert.Contains(expected.Id.ToString(OperationId.FormatSpecifier), actual.Content);
        Assert.Equal(expected.Href.AbsoluteUri, header[0]);
    }

    [Fact]
    public async Task GivenTagPath_WhenCallingApi_ThenShouldReturnOk()
    {
        IMediator mediator = Substitute.For<IMediator>();
        const string path = "11330001";

        var controller = new ExtendedQueryTagController(mediator, NullLogger<ExtendedQueryTagController>.Instance, _featureConfiguration);
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        var expected = new GetExtendedQueryTagErrorsResponse(
            new List<ExtendedQueryTagError>
            {
                new ExtendedQueryTagError(DateTime.UtcNow, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Error 1"),
                new ExtendedQueryTagError(DateTime.UtcNow, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Error 2"),
                new ExtendedQueryTagError(DateTime.UtcNow, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "Error 3"),
            });

        mediator
            .Send(
                Arg.Is<GetExtendedQueryTagErrorsRequest>(x => x.Path == path),
                Arg.Is(controller.HttpContext.RequestAborted))
            .Returns(expected);

        IActionResult response = await controller.GetTagErrorsAsync(path, new PaginationOptions());
        Assert.IsType<ObjectResult>(response);

        var actual = response as ObjectResult;
        Assert.Equal((int)HttpStatusCode.OK, actual.StatusCode);
        Assert.Same(expected.ExtendedQueryTagErrors, actual.Value);

        await mediator.Received(1).Send(
            Arg.Is<GetExtendedQueryTagErrorsRequest>(x => x.Path == path),
            Arg.Is(controller.HttpContext.RequestAborted));
    }

    [Fact]
    public async Task GivenOperationId_WhenAddingTags_ReturnIdWithHeader()
    {
        Guid id = Guid.NewGuid();
        var expected = new AddExtendedQueryTagResponse(
            new OperationReference(id, new Uri("https://dicom.contoso.io/unit/test/Operations/" + id, UriKind.Absolute)));
        IMediator mediator = Substitute.For<IMediator>();
        var controller = new ExtendedQueryTagController(mediator, NullLogger<ExtendedQueryTagController>.Instance, _featureConfiguration);
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        var input = new List<AddExtendedQueryTagEntry>
        {
            new AddExtendedQueryTagEntry
            {
                Level = QueryTagLevel.Instance,
                Path = "00101001",
                VR = DicomVRCode.PN,
            },
            new AddExtendedQueryTagEntry
            {
                Level = QueryTagLevel.Instance,
                Path = "11330001",
                PrivateCreator = "Microsoft",
                VR = DicomVRCode.SS,
            }
        };

        mediator
            .Send(
                Arg.Is<AddExtendedQueryTagRequest>(x => ReferenceEquals(x.ExtendedQueryTags, input)),
                Arg.Is(controller.HttpContext.RequestAborted))
            .Returns(expected);

        var actual = await controller.PostAsync(input) as ObjectResult;
        Assert.NotNull(actual);
        Assert.IsType<OperationReference>(actual.Value);
        Assert.True(controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues header));
        Assert.Single(header);

        Assert.Equal((int)HttpStatusCode.Accepted, actual.StatusCode);
        Assert.Same(expected.Operation, actual.Value);
        Assert.Equal("https://dicom.contoso.io/unit/test/Operations/" + id, header[0]);

        await mediator.Received(1).Send(
            Arg.Is<AddExtendedQueryTagRequest>(x => ReferenceEquals(x.ExtendedQueryTags, input)),
            Arg.Is(controller.HttpContext.RequestAborted));
    }
}
