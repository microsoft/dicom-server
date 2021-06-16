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
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Operations;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ChangeFeed
{
    public class AddExtendedQueryTagServiceTests
    {
        private readonly IExtendedQueryTagEntryValidator _extendedQueryTagEntryValidator;
        private readonly IDicomOperationsClient _client;
        private readonly IAddExtendedQueryTagService _extendedQueryTagService;

        public AddExtendedQueryTagServiceTests()
        {
            _extendedQueryTagEntryValidator = Substitute.For<IExtendedQueryTagEntryValidator>();
            _client = Substitute.For<IDicomOperationsClient>();
            _extendedQueryTagService = new AddExtendedQueryTagService(_extendedQueryTagEntryValidator, _client);
        }

        [Fact]
        public async Task GivenInvalidInput_WhenAddExtendedQueryTagIsInvoked_ThenStopAfterValidation()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            var exception = new ExtendedQueryTagEntryValidationException(string.Empty);
            using var tokenSource = new CancellationTokenSource();

            var input = new AddExtendedQueryTagEntry[] { entry };
            _extendedQueryTagEntryValidator.WhenForAnyArgs(v => v.ValidateExtendedQueryTags(input)).Throw(exception);

            await Assert.ThrowsAsync<ExtendedQueryTagEntryValidationException>(() => _extendedQueryTagService.AddExtendedQueryTagsAsync(input, tokenSource.Token));

            _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
            await _client.DidNotReceiveWithAnyArgs().StartExtendedQueryTagAdditionAsync(default, default);
        }

        [Fact]
        public async Task GivenValidInput_WhenAddExtendedQueryTagIsInvoked_ThenShouldSucceed()
        {
            DicomTag tag = DicomTag.DeviceSerialNumber;
            AddExtendedQueryTagEntry entry = tag.BuildAddExtendedQueryTagEntry();
            using var tokenSource = new CancellationTokenSource();

            var input = new AddExtendedQueryTagEntry[] { entry };
            string expectedOperationId = Guid.NewGuid().ToString();
            _client
                .StartExtendedQueryTagAdditionAsync(
                    Arg.Is<IReadOnlyCollection<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                    Arg.Is(tokenSource.Token))
                .Returns(expectedOperationId);

            Assert.Equal(expectedOperationId, (await _extendedQueryTagService.AddExtendedQueryTagsAsync(input, tokenSource.Token)).OperationId);

            _extendedQueryTagEntryValidator.Received(1).ValidateExtendedQueryTags(input);
            await _client
                .Received(1)
                .StartExtendedQueryTagAdditionAsync(
                    Arg.Is<IReadOnlyCollection<AddExtendedQueryTagEntry>>(x => x.Single().Path == entry.Path),
                    Arg.Is(tokenSource.Token));
        }
    }
}
