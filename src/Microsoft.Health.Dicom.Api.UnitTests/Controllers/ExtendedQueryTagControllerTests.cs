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
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
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
                Substitute.For<IUrlResolver>(),
                Options.Create(new FeatureConfiguration()),
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                null,
                Options.Create(new FeatureConfiguration()),
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                Substitute.For<IUrlResolver>(),
                null,
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                Substitute.For<IUrlResolver>(),
                Options.Create<FeatureConfiguration>(null),
                NullLogger<ExtendedQueryTagController>.Instance));

            Assert.Throws<ArgumentNullException>(() => new ExtendedQueryTagController(
                new Mediator(t => null),
                Substitute.For<IUrlResolver>(),
                Options.Create(new FeatureConfiguration()),
                null));
        }

        [Fact]
        public async Task GivenFeatureIsDisabled_WhenCallingApi_ThenShouldThrowException()
        {
            IMediator _mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var featureConfig = Options.Create(new FeatureConfiguration { EnableExtendedQueryTags = false });
            ExtendedQueryTagController controller = new ExtendedQueryTagController(
                _mediator,
                urlResolver,
                featureConfig,
                NullLogger<ExtendedQueryTagController>.Instance);

            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetTagAsync(DicomTag.PageNumberVector.GetPath()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.GetAllTagsAsync());
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.PostAsync(Array.Empty<AddExtendedQueryTagEntry>()));
            await Assert.ThrowsAsync<ExtendedQueryTagFeatureDisabledException>(() => controller.DeleteAsync(DicomTag.PageNumberVector.GetPath()));
        }

        [Fact]
        public async Task GivenOperationId_WhenAddingTags_ReturnIdWithHeader()
        {
            string id = Guid.NewGuid().ToString();
            string statusUrl = "https://dicom.contoso.io/unit/test/Operations/" + id;
            IMediator mediator = Substitute.For<IMediator>();
            IUrlResolver urlResolver = Substitute.For<IUrlResolver>();
            var controller = new ExtendedQueryTagController(
                mediator,
                urlResolver,
                Options.Create(new FeatureConfiguration { EnableExtendedQueryTags = true }),
                NullLogger<ExtendedQueryTagController>.Instance);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            List<AddExtendedQueryTagEntry> input = new List<AddExtendedQueryTagEntry>
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
                .Returns(new AddExtendedQueryTagResponse(id));
            urlResolver.ResolveOperationStatusUri(id).Returns(new Uri(statusUrl, UriKind.Absolute));

            var actual = await controller.PostAsync(input) as ObjectResult;
            Assert.NotNull(actual);
            Assert.IsType<AddExtendedQueryTagResponse>(actual.Value);
            Assert.True(controller.Response.Headers.TryGetValue(HeaderNames.Location, out StringValues header));
            Assert.Single(header);

            Assert.Equal((int)HttpStatusCode.Accepted, actual.StatusCode);
            Assert.Equal(id, ((AddExtendedQueryTagResponse)actual.Value).OperationId);
            Assert.Equal(statusUrl, header[0]);

            await mediator.Received(1).Send(
                Arg.Is<AddExtendedQueryTagRequest>(x => ReferenceEquals(x.ExtendedQueryTags, input)),
                Arg.Is(controller.HttpContext.RequestAborted));
            urlResolver.Received(1).ResolveOperationStatusUri(id);
        }
    }
}
