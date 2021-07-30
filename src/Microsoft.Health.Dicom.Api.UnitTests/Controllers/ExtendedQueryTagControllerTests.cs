// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Dicom;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Api.Controllers;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models.Operations;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Api.UnitTests.Extensions
{
    public class ExtendedQueryTagControllerTests
    {
        [Fact]
        public void GivenNullArguments_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                null,
                Options.Create(new FeatureConfiguration()),
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                null,
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                Options.Create<FeatureConfiguration>(null),
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                Options.Create(new FeatureConfiguration()),
                null));
        }

        [Fact]
        public async Task GivenFeatureIsDisabled_WhenCallingApi_ThenShouldThrowException()
        {
            IMediator _mediator = Substitute.For<IMediator>();
            IOptions<FeatureConfiguration> featureConfig = Options.Create(new FeatureConfiguration { EnableExtendedQueryTags = false });
            var controller = new ExtendedQueryTagController(
                _mediator,
                featureConfig,
                NullLogger<ExtendedQueryTagController>.Instance);

            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetTagAsync(DicomTag.PageNumberVector.GetPath()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetTagErrorsAsync(DicomTag.PageNumberVector.GetPath()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetAllTagsAsync());
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.PostAsync(Array.Empty<AddExtendedQueryTagEntry>()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.DeleteAsync(DicomTag.PageNumberVector.GetPath()));
        }

        [Fact]
        public async Task GivenTagPath_WhenCallingApi_ThenShouldReturnOk()
        {
            IMediator mediator = Substitute.For<IMediator>();
            const string path = "11330001";

            var controller = new ExtendedQueryTagController(
                mediator,
                Options.Create(new FeatureConfiguration { EnableExtendedQueryTags = true }),
                NullLogger<ExtendedQueryTagController>.Instance);
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

            IActionResult response = await controller.GetTagErrorsAsync(path);
            Assert.IsType<ObjectResult>(response);

            var actual = response as ObjectResult;
            Assert.Equal((int)HttpStatusCode.OK, actual.StatusCode);
            Assert.Same(expected, actual.Value);

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
            var controller = new ExtendedQueryTagController(
                mediator,
                Options.Create(new FeatureConfiguration { EnableExtendedQueryTags = true }),
                NullLogger<ExtendedQueryTagController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var input = new List<AddExtendedQueryTagEntry>
            {
                new AddExtendedQueryTagEntry
                {
                    Level = QueryTagLevel.Instance.ToString(),
                    Path = "00101001",
                    VR = DicomVRCode.PN,
                },
                new AddExtendedQueryTagEntry
                {
                    Level = QueryTagLevel.Instance.ToString(),
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
            Assert.IsType<AddExtendedQueryTagResponse>(actual.Value);
            Assert.True(controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues header));
            Assert.Single(header);

            Assert.Equal((int)HttpStatusCode.Accepted, actual.StatusCode);
            Assert.Same(expected, actual.Value);
            Assert.Equal("https://dicom.contoso.io/unit/test/Operations/" + id, header[0]);

            await mediator.Received(1).Send(
                Arg.Is<AddExtendedQueryTagRequest>(x => ReferenceEquals(x.ExtendedQueryTags, input)),
                Arg.Is(controller.HttpContext.RequestAborted));
        }
    }
}
