// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class AddExtendedQueryTagServiceTests
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomOperationsClient _client;
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly AddExtendedQueryTagService _extendedQueryTagService;
        private readonly IUrlResolver _urlResolver;
        private readonly CancellationTokenSource _tokenSource;

        public AddExtendedQueryTagServiceTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _client = Substitute.For<IDicomOperationsClient>();
            _extendedQueryTagEntryValidator = Substitute.For<IExtendedQueryTagEntryValidator>();
            _urlResolver = Substitute.For<IUrlResolver>();
            _extendedQueryTagService = new AddExtendedQueryTagService(
                _extendedQueryTagStore,
                _client,
                _extendedQueryTagEntryValidator,
                _urlResolver,
                Options.Create(new ExtendedQueryTagConfiguration { MaxAllowedCount = 128 }));

            _tokenSource = new CancellationTokenSource();
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddingExtendedQueryTag_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            var exception = new ExtendedQueryTagEntryValidationException(string.Empty);

            var input = new AddExtendedQueryTagEntry[] { entry };
            _extendedQueryTagEntryValidator.WhenForAnyArgs(v => v.ValidateExtendedQueryTags(input)).Throw(exception);

            await Assert.ThrowsAsync<ExtendedQueryTagEntryValidationException>(
                () => _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token));

            _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
            await _client.DidNotReceiveWithAnyArgs().StartQueryTagIndexingAsync(default, default);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddingExtendedQueryTag_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            ExtendedQueryTagStoreEntry storeEntry = tag.BuildExtendedQueryTagStoreEntry();

            var input = new AddExtendedQueryTagEntry[] { entry };
            string expectedOperationId = Guid.NewGuid().ToString();
            var expectedHref = new Uri("https://dicom.contoso.io/unit/test/Operations/" + expectedOperationId, UriKind.Absolute);
            _extendedQueryTagStore
                .AddExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyList<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                    Arg.Is(128),
                    Arg.Is(false),
                    Arg.Is(_tokenSource.Token))
                .Returns(new List<int> { storeEntry.Key });
            _client
                .StartQueryTagIndexingAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.Single() == storeEntry.Key),
                    Arg.Is(_tokenSource.Token))
                .Returns(expectedOperationId);
            _urlResolver
                .ResolveOperationStatusUri(expectedOperationId)
                .Returns(expectedHref);

            AddExtendedQueryTagResponse actual = await _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token);
            Assert.Equal(expectedOperationId, actual.Operation.Id);
            Assert.Equal(expectedHref, actual.Operation.Href);

            _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
            await _extendedQueryTagStore
                .Received(1)
                .AddExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyList<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                    Arg.Is(128),
                    Arg.Is(false),
                    Arg.Is(_tokenSource.Token));
            await _client
                .Received(1)
                .StartQueryTagIndexingAsync(
                    Arg.Is<IReadOnlyList<int>>(x => x.Single() == storeEntry.Key),
                    Arg.Is(_tokenSource.Token));
            _urlResolver.Received(1).ResolveOperationStatusUri(expectedOperationId);
        }
    }
}
