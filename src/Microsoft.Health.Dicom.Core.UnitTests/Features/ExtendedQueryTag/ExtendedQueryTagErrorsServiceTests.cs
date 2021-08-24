// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class ExtendedQueryTagErrorsServiceTests
    {
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IExtendedQueryTagErrorsService _extendedQueryTagErrorsService;
        private readonly CancellationTokenSource _tokenSource;
        private readonly DateTime _definedNow;

        public ExtendedQueryTagErrorsServiceTests()
        {
            _extendedQueryTagErrorStore = Substitute.For<IExtendedQueryTagErrorStore>();
            _extendedQueryTagErrorsService = new ExtendedQueryTagErrorsService(_extendedQueryTagErrorStore);
            _tokenSource = new CancellationTokenSource();
            _definedNow = DateTime.UtcNow;
        }

        [Fact]
        public async Task GivenValidInput_WhenAddingExtendedQueryTag_ThenShouldSucceed()
        {
            const int tagKey = 7;
            const string errorMessage = "fake error message.";
            const long watermark = 30;

            _extendedQueryTagErrorStore.AddExtendedQueryTagErrorAsync(
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<long>(),
                Arg.Any<CancellationToken>())
            .Returns(tagKey);

            var actual = await _extendedQueryTagErrorsService.AddExtendedQueryTagErrorAsync(
                tagKey,
                errorMessage,
                watermark,
                _tokenSource.Token);

            await _extendedQueryTagErrorStore
                .Received(1)
                .AddExtendedQueryTagErrorAsync(
                    Arg.Is(tagKey),
                    Arg.Is(errorMessage),
                    Arg.Is(watermark),
                    Arg.Is(_tokenSource.Token));
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagDoesNotExist_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };
            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);
            Assert.Empty(response.ExtendedQueryTagErrors);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagHasNoError_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);
            await _extendedQueryTagErrorStore.Received(1).GetExtendedQueryTagErrorsAsync(tagPath);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagExists_ThenReturnExtendedQueryTagErrorsList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            var expected = new List<ExtendedQueryTagError> { new ExtendedQueryTagError(
                DateTime.UtcNow,
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                "fake error message",
                 ExtendedQueryTagErrorStatus.Unacknowledged,
                 DateTime.UtcNow) };

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(expected);
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);
            await _extendedQueryTagErrorStore.Received(1).GetExtendedQueryTagErrorsAsync(tagPath);
            Assert.Equal(expected, response.ExtendedQueryTagErrors);
        }
    }
}
