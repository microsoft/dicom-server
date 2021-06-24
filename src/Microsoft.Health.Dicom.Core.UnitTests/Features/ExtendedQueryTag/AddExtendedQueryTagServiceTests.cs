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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class AddExtendedQueryTagServiceTests
    {
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomOperationsClient _client;
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly AddExtendedQueryTagService _extendedQueryTagService;
        private readonly CancellationTokenSource _tokenSource;

        public AddExtendedQueryTagServiceTests()
        {
            var storeFactory = Substitute.For<IStoreFactory<IExtendedQueryTagStore>>();

            _client = Substitute.For<IDicomOperationsClient>();
            _extendedQueryTagEntryValidator = Substitute.For<IExtendedQueryTagEntryValidator>();
            _extendedQueryTagService = new AddExtendedQueryTagService(
                storeFactory,
                _client,
                _extendedQueryTagEntryValidator,
                Options.Create(new ExtendedQueryTagConfiguration { MaxAllowedCount = 128 }));

            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _tokenSource = new CancellationTokenSource();
            storeFactory.GetInstanceAsync(_tokenSource.Token).Returns(_extendedQueryTagStore);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddExtendedQueryTagIsInvoked_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            var exception = new ExtendedQueryTagEntryValidationException(string.Empty);

            var input = new AddExtendedQueryTagEntry[] { entry };
            _extendedQueryTagEntryValidator.WhenForAnyArgs(v => v.ValidateExtendedQueryTags(input)).Throw(exception);

            await Assert.ThrowsAsync<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token));

            _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
            await _client.DidNotReceiveWithAnyArgs().StartQueryTagIndex(default, default);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddExtendedQueryTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();

            var input = new AddExtendedQueryTagEntry[] { entry };
            string expectedOperationId = Guid.NewGuid().ToString();
            _extendedQueryTagStore
                .AddExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyCollection<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                    Arg.Is(128),
                    Arg.Is(_tokenSource.Token))
                .Returns(new List<int> { 7 });
            _client
                .StartQueryTagIndex(
                    Arg.Is<IReadOnlyCollection<int>>(x => x.Single() == 7),
                    Arg.Is(_tokenSource.Token))
                .Returns(expectedOperationId);

            AddExtendedQueryTagResponse actual = await _extendedQueryTagService.AddExtendedQueryTagsAsync(input, _tokenSource.Token);
            Assert.Equal(expectedOperationId, actual.OperationId);

            _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
            await _extendedQueryTagStore
                .Received(1)
                .AddExtendedQueryTagsAsync(
                    Arg.Is<IReadOnlyCollection<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                    Arg.Is(128),
                    Arg.Is(_tokenSource.Token));
            await _client
                .Received(1)
                .StartQueryTagIndex(
                    Arg.Is<IReadOnlyCollection<int>>(x => x.Single() == 7),
                    Arg.Is(_tokenSource.Token));
        }
    }
}
