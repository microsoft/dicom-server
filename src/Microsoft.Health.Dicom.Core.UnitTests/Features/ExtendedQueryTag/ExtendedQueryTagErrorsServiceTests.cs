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
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.ExtendedQueryTag;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.ExtendedQueryTag
{
    public class ExtendedQueryTagErrorsServiceTests
    {
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IDicomTagParser _dicomTagParser;
        private readonly IExtendedQueryTagErrorsService _extendedQueryTagErrorsService;
        private readonly CancellationTokenSource _tokenSource;
        private readonly DateTime _definedNow;

        public ExtendedQueryTagErrorsServiceTests()
        {
            _extendedQueryTagErrorStore = Substitute.For<IExtendedQueryTagErrorStore>();
            _dicomTagParser = Substitute.For<IDicomTagParser>();
            _extendedQueryTagErrorsService = new ExtendedQueryTagErrorsService(_extendedQueryTagErrorStore, _dicomTagParser);
            _tokenSource = new CancellationTokenSource();
            _definedNow = DateTime.UtcNow;
        }

        [Fact]
        public async Task GivenValidInput_WhenAddingExtendedQueryTag_ThenShouldSucceed()
        {
            const int TagKey = 7;
            const long Watermark = 30;
            const ValidationErrorCode ErrorCode = ValidationErrorCode.DateIsInvalid;

            await _extendedQueryTagErrorsService.AddExtendedQueryTagErrorAsync(
               TagKey,
               ErrorCode,
               Watermark,
               _tokenSource.Token);

            await _extendedQueryTagErrorStore
                .Received(1)
                .AddExtendedQueryTagErrorAsync(
                    Arg.Is(TagKey),
                    Arg.Is(ErrorCode),
                    Arg.Is(Watermark),
                    Arg.Is(_tokenSource.Token));
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagDoesNotExist_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);

            _dicomTagParser.Received(1).TryParse(
                Arg.Is(tagPath),
                out Arg.Any<DicomTag[]>());

            Assert.Empty(response.ExtendedQueryTagErrors);
        }

        [Fact]
        public async Task GivenRequestForExtendedQueryTagError_WhenTagHasNoError_ThenReturnEmptyList()
        {
            string tagPath = DicomTag.DeviceID.GetPath();

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(new List<ExtendedQueryTagError>());
            await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);
            await _extendedQueryTagErrorStore.Received(1).GetExtendedQueryTagErrorsAsync(tagPath);
            _dicomTagParser.Received(1).TryParse(
                Arg.Is(tagPath),
                out Arg.Any<DicomTag[]>());
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
                "fake error message") };

            DicomTag[] parsedTags = new DicomTag[] { DicomTag.DeviceID };

            _dicomTagParser.TryParse(tagPath, out Arg.Any<DicomTag[]>()).Returns(x =>
            {
                x[1] = parsedTags;
                return true;
            });

            _extendedQueryTagErrorStore.GetExtendedQueryTagErrorsAsync(tagPath).Returns(expected);
            GetExtendedQueryTagErrorsResponse response = await _extendedQueryTagErrorsService.GetExtendedQueryTagErrorsAsync(tagPath);
            await _extendedQueryTagErrorStore.Received(1).GetExtendedQueryTagErrorsAsync(tagPath);
            _dicomTagParser.Received(1).TryParse(
                Arg.Is(tagPath),
                out Arg.Any<DicomTag[]>());
            Assert.Equal(expected, response.ExtendedQueryTagErrors);
        }
    }
}
