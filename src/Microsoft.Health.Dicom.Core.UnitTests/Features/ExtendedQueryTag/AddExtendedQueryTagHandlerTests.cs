// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Security.Authorization;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Security;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Models.Operations;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagHandlerTests
    {
        [Fact]
        public void GivenNullArgument_WhenConstructing_ThenThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AddExtendedQueryTagHandler(null, Substitute.For<IAddExtendedQueryTagService>()));

            Assert.Throws<ArgumentNullException>(
                () => new AddExtendedQueryTagHandler(new DisabledAuthorizationService<DataActions>(), null));
        }

        [Fact]
        public async Task GivenNullRequest_WhenHandlingRequest_ThenThrowArgumentNullException()
        {
            IAuthorizationService<DataActions> authService = Substitute.For<IAuthorizationService<DataActions>>();
            IAddExtendedQueryTagService tagService = Substitute.For<IAddExtendedQueryTagService>();
            var handler = new AddExtendedQueryTagHandler(authService, tagService);

            await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null, default));

            await authService.DidNotReceiveWithAnyArgs().CheckAccess(default, default);
            await tagService.DidNotReceiveWithAnyArgs().AddExtendedQueryTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenNoAccess_WhenHandlingRequest_ThenThrowUnauthorizedDicomActionException()
        {
            IAuthorizationService<DataActions> authService = Substitute.For<IAuthorizationService<DataActions>>();
            IAddExtendedQueryTagService tagService = Substitute.For<IAddExtendedQueryTagService>();
            var handler = new AddExtendedQueryTagHandler(authService, tagService);

            using var tokenSource = new CancellationTokenSource();

            authService.CheckAccess(DataActions.ManageExtendedQueryTags, tokenSource.Token).Returns(DataActions.None);
            await Assert.ThrowsAsync<UnauthorizedDicomActionException>(
                () => handler.Handle(
                    new AddExtendedQueryTagRequest(Array.Empty<AddExtendedQueryTagEntry>()),
                    tokenSource.Token));

            await authService.Received(1).CheckAccess(DataActions.ManageExtendedQueryTags, tokenSource.Token);
            await tagService.DidNotReceiveWithAnyArgs().AddExtendedQueryTagsAsync(default, default);
        }

        [Fact]
        public async Task GivenRequest_WhenHandlingRequest_ThenReturnResponse()
        {
            IAuthorizationService<DataActions> authService = Substitute.For<IAuthorizationService<DataActions>>();
            IAddExtendedQueryTagService tagService = Substitute.For<IAddExtendedQueryTagService>();
            var handler = new AddExtendedQueryTagHandler(authService, tagService);

            using var tokenSource = new CancellationTokenSource();

            var input = new List<AddExtendedQueryTagEntry> { new AddExtendedQueryTagEntry() };
            var expected = new AddExtendedQueryTagResponse(new OperationReference(Guid.NewGuid().ToString(), new Uri("https://dicom/operation/status")));
            authService.CheckAccess(DataActions.ManageExtendedQueryTags, tokenSource.Token).Returns(DataActions.ManageExtendedQueryTags);
            tagService
                .AddExtendedQueryTagsAsync(
                    Arg.Is<IEnumerable<AddExtendedQueryTagEntry>>(x => ReferenceEquals(x, input)),
                    Arg.Is(tokenSource.Token))
                .Returns(Task.FromResult(expected));

            AddExtendedQueryTagResponse actual = await handler.Handle(
                new AddExtendedQueryTagRequest(input),
                tokenSource.Token);
            Assert.Same(expected, actual);

            await authService.Received(1).CheckAccess(DataActions.ManageExtendedQueryTags, tokenSource.Token);
            await tagService.Received(1).AddExtendedQueryTagsAsync(
                Arg.Is<IEnumerable<AddExtendedQueryTagEntry>>(x => ReferenceEquals(x, input)),
                Arg.Is(tokenSource.Token));
        }
    }
}
